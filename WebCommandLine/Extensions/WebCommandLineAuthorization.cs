﻿using Microsoft.AspNetCore.Authorization;

namespace WebCommandLine
{
    public class WebCommandLineAuthorization : IAuthorizeData
    {
        public string Policy { get; set; }

        public string Roles { get; set; }

        public string AuthenticationSchemes { get; set; }
    }
}
