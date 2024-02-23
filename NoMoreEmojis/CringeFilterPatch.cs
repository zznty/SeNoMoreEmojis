using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Multiplayer;
using Torch;
using Torch.API;
using Torch.Managers.PatchManager;
using Torch.Managers.PatchManager.MSIL;
using Torch.Utils;
using VRage.Game;
using VRage.Network;

namespace NoMoreEmojis;

[PatchShim]
public static class CringeFilterPatch
{
    private static readonly Regex CringeFilterRegex =
        new("[\u0000\ue030-\ue032]", RegexOptions.Multiline | RegexOptions.IgnoreCase | RegexOptions.Compiled);

    [ReflectedMethodInfo(typeof(MyDedicatedServerBase), "OnConnectedClient")]
    private static readonly MethodInfo TargetMethod = null!;

    [ReflectedMethodInfo(typeof(CringeFilterPatch), nameof(Prefix))]
    private static readonly MethodInfo PrefixMethod = null!;

    [ReflectedMethodInfo(typeof(MyPlayerCollection), nameof(MyPlayerCollection.LoadIdentities),
                         Parameters = new[] { typeof(List<MyObjectBuilder_Identity>) })]
    private static readonly MethodInfo LoadPlayersMethod = null!;

    [ReflectedMethodInfo(typeof(CringeFilterPatch), nameof(Transpiler))]
    private static readonly MethodInfo TranspilerMethod = null!;

    [ReflectedMethodInfo(typeof(CringeFilterPatch), nameof(CringeFilter))]
    private static readonly MethodInfo CringeFilterMethod = null!;

    [ReflectedFieldInfo(typeof(MyObjectBuilder_Identity), nameof(MyObjectBuilder_Identity.DisplayName))]
    private static readonly FieldInfo IdentityNameField = null!;

    [ReflectedMethodInfo(typeof(MyPlayerCollection), "OnNewPlayerRequest")]
    private static readonly MethodInfo PlayerRequestMethod = null!;
    
    [ReflectedMethodInfo(typeof(CringeFilterPatch), nameof(PlayerRequestPrefix))]
    private static readonly MethodInfo PlayerRequestPrefixMethod = null!;

    public static void Patch(PatchContext context)
    {
#pragma warning disable CS0618
        if (TorchBase.Instance.Config.UgcServiceType is UGCServiceType.EOS)
#pragma warning restore CS0618
            return;
        context.GetPattern(TargetMethod).Prefixes.Add(PrefixMethod);
        context.GetPattern(LoadPlayersMethod).Transpilers.Add(TranspilerMethod);
        context.GetPattern(PlayerRequestMethod).Prefixes.Add(PlayerRequestPrefixMethod);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void PlayerRequestPrefix(ref MyPlayerCollection.NewPlayerRequestParameters parameters)
    {
        parameters.DisplayName = CringeFilter(parameters.DisplayName);
    }

    private static IEnumerable<MsilInstruction> Transpiler(IEnumerable<MsilInstruction> instructions)
    {
        foreach (var instruction in instructions)
        {
            yield return instruction;
            if (instruction.OpCode == OpCodes.Ldloc_1)
            {
                yield return new(OpCodes.Dup);
                yield return new(OpCodes.Dup);
                yield return new MsilInstruction(OpCodes.Ldfld).InlineValue(IdentityNameField);
                yield return new MsilInstruction(OpCodes.Call).InlineValue(CringeFilterMethod);
                yield return new MsilInstruction(OpCodes.Stfld).InlineValue(IdentityNameField);
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string CringeFilter(string str) => CringeFilterRegex.Replace(str, string.Empty);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Prefix(ref ConnectedClientDataMsg msg) =>
        msg.Name = CringeFilter(msg.Name);
}