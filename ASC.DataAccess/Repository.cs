using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using ASC.DataAccess.Interfaces;
using ASC.Models.BaseTypes;
using ASC.Utilities;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

namespace ASC.DataAccess
{
    public class Repository<T> : IRepository<T> where T : TableEntity, new()
    {
        private readonly CloudStorageAccount storageAccount;
        private readonly CloudTableClient tableClient;
        private readonly CloudTable storageTable;

        public IUnitOfWork Scope { get; set; }

        public Repository(IUnitOfWork scope)
        {

            storageAccount = CloudStorageAccount.Parse(scope.ConnectionString);

            tableClient = storageAccount.CreateCloudTableClient();
            CloudTable table = tableClient.GetTableReference(typeof(T).Name);

            this.storageTable = table;
            this.Scope = scope;
        }

        public async Task<IEnumerable<T>> FindAllByQuery(string query)
        {
            TableContinuationToken tableContinuationToken = null;

            var result =
                await storageTable.ExecuteQuerySegmentedAsync(new TableQuery<T>().Where(query), tableContinuationToken);

            return result.Results as IEnumerable<T>;
        }

        public async Task<T> AddAsync(T entity)
        {
            var entityToInsert = entity as BaseEntity;
            entityToInsert.CreatedDate = DateTime.UtcNow;
            entityToInsert.UpdatedDate = DateTime.UtcNow;

            TableOperation insertOperation = TableOperation.Insert(entity);
            var result = await ExecuteAsync(insertOperation);
            return result.Result as T;
        }

        public async Task<T> UpdateAsync(T entity)
        {
            var entityToUpdate = entity as BaseEntity;
            entityToUpdate.UpdatedDate = DateTime.UtcNow;

            TableOperation updateOperation = TableOperation.Replace(entity);
            var result = await ExecuteAsync(updateOperation);
            return result.Result as T;
        }

        public async Task DeleteAsync(T entity)
        {
            var entityToDelete = entity as BaseEntity;
            entityToDelete.UpdatedDate = DateTime.UtcNow;
            entityToDelete.IsDeleted = true;

            TableOperation deleteOperation = TableOperation.Replace(entityToDelete);
            await ExecuteAsync(deleteOperation);
        }

        #region Implement Find

        public async Task<T> FindAsync(string partitionKey, string rowKey)
        {
            TableOperation retrieveOperation = TableOperation.Retrieve<T>(partitionKey, rowKey);
            var result = await storageTable.ExecuteAsync(retrieveOperation);
            return result.Result as T;
        }

        public async Task<IEnumerable<T>> FindAllByPartitionKeyAsync(string partitionkey)
        {
            TableQuery<T> query = new TableQuery<T>().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, partitionkey));
            TableContinuationToken tableContinuationToken = null;

            var result = await storageTable.ExecuteQuerySegmentedAsync(query, tableContinuationToken);
            return result.Results as IEnumerable<T>;
        }

        public async Task<IEnumerable<T>> FindAllAsync()
        {
            TableQuery<T> query = new TableQuery<T>();
            TableContinuationToken tableContinuationToken = null;
            var result = await storageTable.ExecuteQuerySegmentedAsync(query, tableContinuationToken);

            return result.Results as IEnumerable<T>;
        }

        public async Task CreateTableAsync()
        {
            CloudTable table = tableClient.GetTableReference(typeof(T).Name);
            await table.CreateIfNotExistsAsync();

            if (typeof(IAuditTracker).IsAssignableFrom(typeof(T)))
            {
                var auditTable = tableClient.GetTableReference($"{typeof(T).Name}Audit");
                await auditTable.CreateIfNotExistsAsync();
            }
        }

        private async Task<TableResult> ExecuteAsync(TableOperation operation)
        {
            Task<Action> rollbackAction = CreateRollbackAction(operation);
            TableResult result = await storageTable.ExecuteAsync(operation);

            Scope.RollbackActions.Enqueue(rollbackAction);

            //Implement Audit
            if (operation.Entity is IAuditTracker)
            {
                //Da budemo sigurno da ne koristimo isti RowKex i PartitionKey
                var auditEntity = operation.Entity.CopyObject<T>();
                auditEntity.PartitionKey = $"{auditEntity.PartitionKey}-{auditEntity.RowKey}";
                auditEntity.RowKey = $"{DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fff")}";

                var auditOperation = TableOperation.Insert(auditEntity);
                var auditRollbackAction = CreateRollbackAction(auditOperation, true);

                var auditTable = tableClient.GetTableReference($"{typeof(T).Name}Audit");
                await auditTable.ExecuteAsync(auditOperation);

                Scope.RollbackActions.Enqueue(auditRollbackAction);
            }
            return result;

        }
        #endregion

        #region Implement Rollback

        private async Task<Action> CreateRollbackAction(TableOperation operation, bool IsAuditOperation = false)
        {
            if (operation.OperationType == TableOperationType.Retrieve) return null;

            ITableEntity tableEntity = operation.Entity;
            CloudTable cloudTable = !IsAuditOperation
                ? storageTable
                : tableClient.GetTableReference($"{typeof(T).Name}Audit");


            switch (operation.OperationType)
            {
                case TableOperationType.Insert:
                    return async () => await UndoInsertOperationAsync(cloudTable, tableEntity);
                case TableOperationType.Delete:
                    return async () => await UndoDeleteOperation(cloudTable, tableEntity);
                case TableOperationType.Replace:
                    TableResult retriveResult =
                        await cloudTable.ExecuteAsync(TableOperation.Retrieve(tableEntity.PartitionKey,
                            tableEntity.RowKey));
                    return async () => await UndoReplaceOperation(cloudTable, retriveResult.Result as DynamicTableEntity, tableEntity);
                default:
                    throw new InvalidOperationException("The storage operation cannot be identified.");
            }
        }

        private async Task UndoInsertOperationAsync(CloudTable cloudTable, ITableEntity tableEntity)
        {
            var deleteOperation = TableOperation.Delete(tableEntity);
            await cloudTable.ExecuteAsync(deleteOperation);
        }


        private async Task UndoDeleteOperation(CloudTable cloudTable, ITableEntity tableEntity)
        {
            BaseEntity entityToRestore = tableEntity as BaseEntity;
            entityToRestore.IsDeleted = false;

            var insertOperation = TableOperation.Replace(tableEntity);
            await cloudTable.ExecuteAsync(insertOperation);
        }

        private async Task UndoReplaceOperation(CloudTable cloudTable, DynamicTableEntity originalEntity, ITableEntity tableEntity)
        {
            if (originalEntity != null)
            {
                if (!String.IsNullOrEmpty(tableEntity.ETag))
                {
                    originalEntity.ETag = tableEntity.ETag;
                }
                var replaceOperation = TableOperation.Replace(originalEntity);
                await cloudTable.ExecuteAsync(replaceOperation);
            }
        }

        public async Task<IEnumerable<T>> FindAllInAuditByQuery(string query)
        {
            var auditTable = tableClient.GetTableReference($"{typeof(T).Name}Audit");
            TableContinuationToken tableContinuationToken = null;
            var result = await auditTable.ExecuteQuerySegmentedAsync(new TableQuery<T>().Take(20).Where(query), tableContinuationToken);
            return result.Results as IEnumerable<T>;
        }
        #endregion

    }
}
