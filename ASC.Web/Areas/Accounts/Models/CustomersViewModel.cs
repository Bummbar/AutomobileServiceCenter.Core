﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ASC.Web.Models;

namespace ASC.Web.Areas.Accounts.Models
{
    public class CustomersViewModel
    {
        public List<ApplicationUser> Customers { get; set; }
        public CustomerRegistrationViewModel Registration { get; set; }
    }
}
