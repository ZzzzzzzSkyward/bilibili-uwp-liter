            ShieldRegulars = SettingHelper.GetValue<ObservableCollection<string>>(SettingHelper.VideoDanmaku.SHIELD_REGULAR, new ObservableCollection<string>() { });
            var validRegulars = ShieldRegulars.Where(IsValidDanmuRegex).ToList();
            if (validRegulars.Count != ShieldRegulars.Count)
            {
                ShieldRegulars = new ObservableCollection<string>(validRegulars);
                SettingHelper.SetValue(SettingHelper.VideoDanmaku.SHIELD_REGULAR, ShieldRegulars);
            }
