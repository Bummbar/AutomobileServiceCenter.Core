﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace ASC.Tests.TestUtilities
{
    public class FakeSession : ISession
    {
        private Dictionary<string,byte[]> sessionFactory = new Dictionary<string, byte[]>();

        public bool IsAvailable => throw new NotImplementedException();

        public string Id => throw new NotImplementedException();

        public IEnumerable<string> Keys => throw new NotImplementedException();

        public void Clear()
        {
            throw new NotImplementedException();
        }

        public Task CommitAsync()
        {
            throw new NotImplementedException();
        }

        public Task LoadAsync()
        {
            throw new NotImplementedException();
        }

        public void Remove(string key)
        {
            throw new NotImplementedException();
        }

        public void Set(string key, byte[] value)
        {
            if (!sessionFactory.ContainsKey(key))
            {
                sessionFactory.Add(key,value);
            }
            else
            {
                sessionFactory[key] = value;
            }

        }

        public bool TryGetValue(string key, out byte[] value)
        {
            if (sessionFactory.ContainsKey(key) && sessionFactory[key] != null)
            {
                value = sessionFactory[key];
                return true;
            }
            else
            {
                value = null;
                return false;
            }
        }
    }
}
