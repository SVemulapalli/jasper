﻿using System;
using System.Linq.Expressions;
using Alba;
using Jasper.Http.Routing;

namespace Jasper.TestSupport.Alba
{
    public class JasperUrlLookup : IUrlLookup
    {
        private readonly IUrlRegistry _urls;

        public JasperUrlLookup(IUrlRegistry urls)
        {
            _urls = urls;
        }

        public string UrlFor<T>(Expression<Action<T>> expression, string httpMethod)
        {
            return _urls.UrlFor(expression, httpMethod);
        }

        public string UrlFor<T>(string method)
        {
            return _urls.UrlFor<T>(method);
        }

        public string UrlFor<T>(T input, string httpMethod)
        {
            return _urls.UrlFor(input, httpMethod);
        }
    }
}
