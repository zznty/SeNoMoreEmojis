using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using Sandbox.Definitions;
using Torch.Managers.PatchManager;
using Torch.Managers.PatchManager.MSIL;
using Torch.Utils;
using VRage.Utils;

namespace NoMoreEmojis;

[PatchShim]
public static class DefinitionLogSpamPatch
{
    [ReflectedMethodInfo(typeof(MyDefinitionManager), "ProcessContentFilePath")]
    private static readonly MethodInfo ProcessMethod = null!;

    [ReflectedMethodInfo(typeof(DefinitionLogSpamPatch), nameof(Transpiler))]
    private static readonly MethodInfo TranspilerMethod = null!;

    [ReflectedMethodInfo(typeof(Directory), nameof(Directory.Exists))]
    private static readonly MethodInfo DirectoryExistsMethod = null!;

    public static void Patch(PatchContext context)
    {
        context.GetPattern(ProcessMethod).Transpilers.Add(TranspilerMethod);
    }

    private static IEnumerable<MsilInstruction> Transpiler(IEnumerable<MsilInstruction> instructions)
    {
        var ins = instructions.ToList();
        
        var idx = ins.FindIndex(b => b.OpCode == OpCodes.Call && b.Operand is MsilOperandInline.MsilOperandReflected<MethodBase> { Value.Name: "Exists" });
        ins[idx].InlineValue(DirectoryExistsMethod);
        
        return ins;
    } 
}