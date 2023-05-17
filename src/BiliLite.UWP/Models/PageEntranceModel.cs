﻿using BiliLite.Helpers;
using System;

namespace BiliLite.Models
{
    public class PageEntranceModel
    {
        public PageEntranceModel()
        {
        }
        public PageEntranceModel(string name,string logo, NavigationInfo navigationInfo)
        {
            GUID = Guid.NewGuid().ToString();
            Logo = logo;
            Name = name;
            NavigationInfo = navigationInfo;
        }
        public string GUID { get; set; }
        public string Logo { get; set; }
        public string Name { get; set; }
        public NavigationInfo NavigationInfo { get; set; }
    }
}
