﻿using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace Jasper.Http.Routing
{
    public interface IUrlRegistry
    {
        string UrlFor(object model, string httpMethod = null);

        string UrlFor<T>(string httpMethod = null);

        string UrlFor(Type handlerType, MethodInfo method = null, string httpMethod = null);

        string UrlFor<THandler>(Expression<Action<THandler>> expression, string httpMethod = null);

        string UrlFor(string routeName, IDictionary<string, object> parameters = null);

        string UrlFor(Type handlerType, string methodName);
    }
}
