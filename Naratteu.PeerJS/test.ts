import { Peer } from "https://esm.sh/peerjs@1.5.4?bundle-deps"

export function echo(msg: string): string
{
    return msg;
}

type openPeerHelper = any;
export function openPeer(dotNetHelper: openPeerHelper): Peer
{
    const peer = new Peer();
    peer.on('open', (id) => { dotNetHelper.invokeMethodAsync('on_open', id); });
    return peer;
}

export function connPeer(peer: Peer, id: string, dotNetHelper: openPeerHelper): Peer
{
    const conn = peer.connect(id);
    conn.on('open', () => { dotNetHelper.invokeMethodAsync('on_open', id); });
    return conn;
}

type servPeerHelper = any;
export function servPeer(peer: Peer, dotNetHelper: servPeerHelper): Peer
{
    peer.on('connection', (conn) => {
        conn.on('open', () => { dotNetHelper.invokeMethodAsync('on_open', conn.peer); });
        conn.on('data', (data) => { dotNetHelper.invokeMethodAsync('on_data', data); });
    });
    return peer;
}

export function send(conn: Peer, msg: string): string
{
    conn.send(msg);
    return msg;
}

export function createConnectionUrl(id: string): string
{
    return window.location.origin + window.location.pathname + "?conn_to=" + id;
}