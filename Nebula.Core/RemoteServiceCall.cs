﻿using Castle.DynamicProxy;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace Nebula.Core
{
    [DataContract]
    public class RemoteServiceCall
    {
        [DataMember]
        private Guid _id;
        [DataMember]
        private ISession _session;

        [DataMember]
        private string _typeFullName;
        [DataMember]
        private string _methodName;

        [DataMember]
        private object[] _parameters;
        [DataMember]
        private Type[] _genericArguments;
        [DataMember]
        private Type _returnType;

        [DataMember]
        private bool _isGenericMethod;

        public RemoteServiceCall()
        {

        }

        public RemoteServiceCall(ISession session, IInvocation invocation)
        {
            if (session == null)
                throw new ArgumentNullException(nameof(session));

            _id = Guid.NewGuid();
            _session = session;

            //  A bit of an abuse of LINQ really.
            var remoteServiceAttributeValue = invocation.Method.DeclaringType.CustomAttributes.
                FirstOrDefault(x => x.AttributeType == typeof(RemoteServiceAttribute)).ConstructorArguments.
                FirstOrDefault().Value;

            _typeFullName = remoteServiceAttributeValue.ToString();
            _methodName = invocation.Method.Name;
            _parameters = invocation.Arguments;
            _genericArguments = invocation.GenericArguments;
            _returnType = invocation.Method.ReturnType;
            _isGenericMethod = invocation.Method.IsGenericMethod;
        }

        public Type ReturnType
        {
            get { return _returnType; }
        }

        public async Task<RemoteServiceResponse> ExecuteRemotely()
        {
            var serialized = SerializationHelper.SerializeToJson(this);

            var appServicePrefix = ConfigurationManager.AppSettings.GetValues("ApplicationServicePrefix").FirstOrDefault();

            using (var client = new HttpClient())
            {
                var content = new StringContent(serialized);

                var response = await client.PostAsync(appServicePrefix, content);

                var responseBytes = await response.Content.ReadAsByteArrayAsync();

                var responseString = System.Text.Encoding.UTF8.GetString(responseBytes);

                var deserialized = SerializationHelper.DeserializeFromJson<RemoteServiceResponse>(responseString);

                if (deserialized.Exception != null)
                {
                    throw deserialized.Exception.InnerException;
                }

                return deserialized;
            }
        }

        private MethodInfo GetMethodWithMatchingParameters(List<MethodInfo> methods)
        {
            MethodInfo method = methods.FirstOrDefault();

            foreach (var candidateMethod in methods)
            {
                method = GetClosestMatchingMethod(candidateMethod, method);

                if (method != null)
                    break;
            }

            if (method == null)
            {
                throw new Exception($"Unable to find method {method.Name} with matching parameters.");
            }

            return method;
        }

        private MethodInfo GetClosestMatchingMethod(MethodInfo candidateMethod, MethodInfo method)
        {
            var pars = candidateMethod.GetParameters();

            var argc = _parameters.Count();

            for (int i = 0; i < argc; ++i)
            {
                var par = _parameters[i].GetType();
                var methDefPar = pars[i].ParameterType;

                if (methDefPar.IsAssignableFrom(par) || methDefPar.IsGenericParameter)
                {
                    method = candidateMethod;
                }
                else
                {
                    method = null;
                    break;
                }
            }

            return method;
        }

        public object GetResult()
        {
            var type = TypeRegistry.Instance.GetRegisteredTypeByName(_typeFullName);

            var instance = Activator.CreateInstance(type);

            if (_isGenericMethod)
            {
                return InvokeGenericMethod(type, instance);
            }
            else
            {
                return InvokeNonGenericMethod(type, instance);
            }
        }

        private object InvokeNonGenericMethod(Type type, object instance)
        {
            var methods = type.GetMethods().Where(x => x.Name == _methodName &&
                                                    !x.IsGenericMethod &&
                                                    x.ReturnType == _returnType &&
                                                    x.GetParameters().Count() == _parameters.Count()).ToList();

            var candidateMethod = GetMethodWithMatchingParameters(methods);

            return candidateMethod.Invoke(instance, _parameters);
        }

        private object InvokeGenericMethod(Type type, object instance)
        {
            var methods = type.GetMethods().Where(x => x.Name == _methodName && x.IsGenericMethod &&
                                                    x.GetParameters().Count() == _parameters.Count() &&
                                                    x.GetGenericArguments().Count() == _genericArguments.Count()).ToList();

            var candidateMethod = GetMethodWithMatchingParameters(methods);

            var method = candidateMethod.MakeGenericMethod(_genericArguments);
            return method.Invoke(instance, _parameters);
        }
    }
}
