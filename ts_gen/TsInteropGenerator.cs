using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Text;
using System.Text.RegularExpressions;
using System.CodeDom.Compiler;

namespace ts_gen;

[Generator]
public class TsInteropGenerator : IIncrementalGenerator
{
    void IIncrementalGenerator.Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterSourceOutput(context.AdditionalTextsProvider.Where(text => text.Path.EndsWith(".ts")), (ctx, ts) =>
        {
            var exports = from line in ts.GetText()?.Lines
                          let match = Regex.Match(line.ToString(), @"export function ([\w_]+)\(([\w_ :,]*)\): ([\w_]+)")
                          let npr = match.Groups is [_, { Value: var n }, { Value: var p }, { Value: var r }] ? (n, p, r) : default
                          where match.Success && npr != default
                          select npr;
            using var output = new StringWriter();
            using var writer = new IndentedTextWriter(output);
            writer.Indent = 2;
            foreach (var (name, paramters, returntype) in exports ?? [])
            {
                var p = paramters.Split(',').Select(p => Regex.Match(p, @"^ *([\w_]+) *: *([\w_]+) *$").Groups is [_, { Value: var name }, { Value: var type }] ? (name, type) : default).Where(nt => nt != default);
                var p1 = string.Join(", ", p.Select(nt => $"{nt.type} {nt.name}"));
                var p2 = string.Join(", ", [$"nameof({name})", .. p.Select(nt => nt.name)]);
                writer.WriteLine($"""public async ValueTask<{returntype}> {name}({p1}) => await (await moduleTask.Value).InvokeAsync<{returntype}>({p2});""");
            }
            var filename = Path.GetFileNameWithoutExtension(ts.Path);
            ctx.AddSource($"{filename}.g.cs", SourceText.From($$"""
            using System;
            using Microsoft.JSInterop;

            namespace Naratteu.PeerJS
            {
                public partial class {{filename}}(IJSRuntime jsRuntime) : IAsyncDisposable
                {
                    readonly Lazy<Task<IJSObjectReference>> moduleTask = new(() => jsRuntime.InvokeAsync<IJSObjectReference>("import", "./_content/Naratteu.PeerJS/{{filename}}.js").AsTask());
                        
                    {{output.GetStringBuilder()}}
                    async ValueTask IAsyncDisposable.DisposeAsync()
                    {
                        if (moduleTask.IsValueCreated)
                            await (await moduleTask.Value).DisposeAsync();
                    }
                }
            }
            """, Encoding.UTF8));
        });
    }
}