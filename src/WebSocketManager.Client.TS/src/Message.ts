export enum MessageType {
    Text = 0,
    MethodInvocation = 1,
    ConnectionEvent = 2
}

export class Message {
    public messageType: MessageType;
    public data: string;
}
