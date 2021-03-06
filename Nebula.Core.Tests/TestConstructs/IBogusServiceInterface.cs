﻿using System;
using System.Threading.Tasks;

namespace Nebula.Core.Tests.TestConstructs
{
    [RemoteService("BogusService")]
    public interface IBogusServiceInterface
    {
        string ReturnBogusString();
        string ReturnBogusString(string input);
        Task<string> ReturnBogusStringAsync(string input);
        string ReturnBogusString(string input, long blah);

        Task DoSomethingAsync();

        GenericType ReturnBogusGenericObject<GenericType>();
        GenericType ReturnBogusGenericObject<GenericType>(GenericType input);
        GenericType ReturnBogusGenericObject<GenericType, GenericType2>(GenericType input, GenericType2 input2);

        bool ReturnBogusBool(BogusTrackable a);
        IBogusTrackable ReturnBogusObject(IBogusTrackable input);

        void ThrowAnException(string exceptionMessage);

        string SlowServiceCall(string input);
        Task<string> SlowServiceCallAsync(string input);

        Guid? ReturnNullableType(bool returnNull);

        Guid ReturnGuid();
    }
}
