using BepInEx.Logging;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Antiban;
public static class Patcher
{
    static ManualLogSource Logger { get; set; } = BepInEx.Logging.Logger.CreateLogSource("Antiban");
    static readonly Dictionary<string, string> _keywords = new()
    {
        ["PNN"] = "Photon.Pun.PhotonNetwork",
        ["LBC"] = "Photon.Realtime.LoadBalancingClient"
    };
    static readonly string[] _blockedMethods = ["PNN.Destroy", "PNN.SendDestroyOfPlayer", "PNN.SendDestroyOfAll", "PNN.DestroyPlayerObjects", "PNN.DestroyAll", "PNN.SetMasterClient", "PNN.GetCustomRoomList", "PNN.FindFriends", "LBC.OpGetGameList", "LBC.OpFindFriends"];
    static string[] BlockedMethods => [.. _blockedMethods.Select(CheckKeywords)];
    static string CheckKeywords(string input)
    {
        foreach ((string key, string value) in _keywords)
        {
            input = input.Replace(key, value);
        }

        return input;
    }
    public static IEnumerable<string> TargetDLLs => ["PhotonUnityNetworking.dll", "PhotonRealtime.dll"];
    public static void Patch(AssemblyDefinition assembly)
    {
        foreach (ModuleDefinition module in assembly.Modules)
        {
            foreach (TypeDefinition type in module.Types)
            {
                foreach (MethodDefinition method in type.Methods)
                {
                    if (BlockedMethods.Contains($"{type.FullName}.{method.Name}"))
                    {
                        ILProcessor IL = method.Body.GetILProcessor();
                        string methodSignature = $"{type.FullName}.{method.Name}({string.Join(", ", method.Parameters.Select(P => P.ParameterType.Name))})";

                        IL.Body.Instructions.Clear();
                        IL.Emit(OpCodes.Ldstr, $"[ANTIBAN] {methodSignature} is a bannable method");
                        IL.Emit(OpCodes.Newobj, module.ImportReference(typeof(Exception).GetConstructor([typeof(string)])));
                        IL.Emit(OpCodes.Throw);
                        Logger.LogInfo($"Patched method: {methodSignature}");
                    }
                }
            }
        }
    }

}
