// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


namespace IdentityServer4.Quickstart.UI
{
    public class ConsentOptions
    {
        public static bool EnableOfflineAccess = true;
        public static string OfflineAccessDisplayName = "离线访问权限";
        public static string OfflineAccessDescription = "在你离线时，仍然可以代替你访问你的信息和资源";

        public static readonly string MustChooseOneErrorMessage = "你至少应选择一项权限";
        public static readonly string InvalidSelectionErrorMessage = "选择项无效";
    }
}
