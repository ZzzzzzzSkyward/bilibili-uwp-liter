                    {
                        var users = items.Where(x => x.type == 1).Select(x => x.filter).ToList();
                        var ls = ShieldRegulars.Union(users);
                        ShieldRegulars.Clear();
                        foreach (var item in ls)
                        {
                            ShieldRegulars.Add(item);
                        }
                        SettingHelper.SetValue(SettingHelper.VideoDanmaku.SHIELD_REGULAR, ShieldRegulars);
                    }
