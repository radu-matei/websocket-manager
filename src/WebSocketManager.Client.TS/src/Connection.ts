import { InvocationDescriptor } from './InvocationDescriptor'
import { Message, MessageType } from './Message'

export class Connection {
    
    public url: string;
    public connectionId: string;

    protected message: Message;
    protected socket: WebSocket;

    public clientMethods: { [s: string]: Function; } = {};

    constructor(url: string) {
        this.url = url;
    }

    public start() {
        this.socket = new WebSocket(this.url);

        this.socket.onopen = (event: MessageEvent) =>  {
            console.log('Connected!');
        };

            this.socket.onmessage = (event: MessageEvent) => {
                this.message = JSON.parse(event.data);
                console.log(this.message);

                if(this.message.messageType == MessageType.Text) {
                    console.log('Text message received. Message: ' + this.message.data);
                }

                else if(this.message.messageType == MessageType.MethodInvocation) {
                    let invocationDescriptor: InvocationDescriptor = JSON.parse(this.message.data);

                    this.clientMethods[invocationDescriptor.methodName].apply(this, invocationDescriptor.arguments);
                }

                else if(this.message.messageType == MessageType.ConnectionEvent) {
                    this.connectionId = this.message.data;

                    console.log('Connected! connectionId: ' + this.connectionId);
                }
            }

            this.socket.onclose = (event: CloseEvent) => {
                console.log('Connection closed from: ' + this.url);
            }

            this.socket.onerror = (event: ErrorEvent) => {
                console.log('Error data: ' + event.error);
            }
    }
}