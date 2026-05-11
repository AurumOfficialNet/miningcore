using System;
using System.Reflection;
using System.Runtime.ExceptionServices;
using Miningcore.Configuration;
using NLog;
using Xunit;

namespace Miningcore.Tests.Util;

internal static class ProgramTestReflection
{
    public static void SetProgramClusterConfig(ClusterConfig config)
    {
        SetProgramPrivateStaticField("clusterConfig", config);
    }

    public static void SetProgramLogger()
    {
        SetProgramPrivateStaticField("logger", LogManager.GetCurrentClassLogger());
    }

    public static void InvokeProgramPrivateStatic(string methodName, params object[] args)
    {
        var method = typeof(Program).GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic);
        Assert.NotNull(method);

        try
        {
            method.Invoke(null, args);
        }
        catch(TargetInvocationException ex) when(ex.InnerException != null)
        {
            ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
            throw;
        }
    }

    private static void SetProgramPrivateStaticField(string fieldName, object value)
    {
        var field = typeof(Program).GetField(fieldName, BindingFlags.Static | BindingFlags.NonPublic);
        Assert.NotNull(field);
        field.SetValue(null, value);
    }
}