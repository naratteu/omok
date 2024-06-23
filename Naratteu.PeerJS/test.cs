global using Peer = Microsoft.JSInterop.IJSObjectReference;
global using openPeerHelper = Microsoft.JSInterop.DotNetObjectReference<Naratteu.PeerJS.OnOpen>;
global using servPeerHelper = Microsoft.JSInterop.DotNetObjectReference<Naratteu.PeerJS.OnServ>;
using Microsoft.JSInterop;

namespace Naratteu.PeerJS;

partial class test
{
    public async ValueTask<(PeerWrap pw, string id)> OpenPeer()
    {
        TaskCompletionSource<string> tcs = new();
        using var oo = DotNetObjectReference.Create(new OnOpen(tcs.SetResult));
        return (new() { peer = await openPeer(oo), test = this }, await tcs.Task);
    }
}

public class OnOpen(Action<string> oo) { [JSInvokable] public void on_open(string id) => oo(id); }
public class OnServ
{
    public required Action<string> oo, od;
    [JSInvokable] public void on_open(string id) => oo(id);
    [JSInvokable] public void on_data(string data) => od(data);
}

public class PeerWrap : IAsyncDisposable
{
    ValueTask IAsyncDisposable.DisposeAsync() => peer.DisposeAsync();
    public required Peer peer { get; init; }
    public required test test { get; init; }

    public async ValueTask<servPeerHelper> Serv(OnServ on_serv)
    {
        var os = DotNetObjectReference.Create(on_serv);
        await test.servPeer(peer, os);
        return os;
    }

    public async ValueTask<(PeerWrap pw, string id)> Conn(string id)
    {
        TaskCompletionSource<string> tcs = new();
        using var oo = DotNetObjectReference.Create(new OnOpen(tcs.SetResult));
        return (new() { peer = await test.connPeer(peer, id, oo), test = test }, await tcs.Task);
    }

    public async ValueTask<string> Send(string msg) => await test.send(peer, msg);
}