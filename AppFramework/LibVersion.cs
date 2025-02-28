﻿using System;
using System.Reflection;

namespace CFIT.AppFramework
{
    public static class LibVersion
    {
        public static Version Version { get { return Assembly.GetExecutingAssembly()?.GetName()?.Version ?? new Version(); } }
    }
}
