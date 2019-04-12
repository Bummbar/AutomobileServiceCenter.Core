using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Mvc;

namespace ASC.Utilities.Navigation
{

    //Ne zaboraviti da se View postav njegovo property na embedded da se instalira ekstenzija
    //sa kojom cemo postaviti View dstupan glavnoj aplikaciji 
    //takodje svojstvo od JSON fajla treba staviti copy always
    [ViewComponent(Name = "ASC.Utilities.Navigation.LeftNavigation")]
    public class LeftNavigationViewComponent:ViewComponent
    {
        public IViewComponentResult Invoke(NavigationMenu menu)
        {
            menu.MenuItems = menu.MenuItems.OrderBy(p => p.Sequence).ToList();
            return View(menu);
        }
    }
}
