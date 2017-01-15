import { InvocationDescriptor } from './InvocationDescriptor'
import { Message, MessageType } from './Message'

export class Connection {

    public url: string;
    public connectionId: string;
    public enableLogging: boolean = false;

    protected message: Message;
    protected socket: WebSocket;

    public clientMethods: { [s: string]: Function; } = {};
    public connectionMethods: { [s: string]: Function; } = {};

    constructor(url: string, enableLogging: boolean=false) {
        this.url = url;
        
        this.enableLogging = enableLogging;

        this.connectionMethods['onConnected'] = () => {
            if(this.enableLogging) {
                console.log('Connected! connectionId: ' + this.connectionId);
            }
        }

        this.connectionMethods['onDisconnected'] = () => {
            if(this.enableLogging) {
                console.log('Connection closed from: ' + this.url);
            }
        }

        this.connectionMethods['onOpen'] = (socketOpenedEvent: any) => {
            if(this.enableLogging) {
                console.log('WebSockets connection opened!');
            }
        }
    }

    public start() {
        this.socket = new WebSocket(this.url);

        this.socket.onopen = (event: MessageEvent) => {
            this.connectionMethods['onOpen'].apply(this, event);
        };

        this.socket.onmessage = (event: MessageEvent) => {
            this.message = JSON.parse(event.data);

            if (this.message.messageType == MessageType.Text) {
                if(this.enableLogging) {
                    console.log('Text message received. Message: ' + this.message.data);
                }
            }

            else if (this.message.messageType == MessageType.MethodInvocation) {
                let invocationDescriptor: InvocationDescriptor = JSON.parse(this.message.data);

                this.clientMethods[invocationDescriptor.methodName].apply(this, invocationDescriptor.arguments);
            }

            else if (this.message.messageType == MessageType.ConnectionEvent) {
                this.connectionId = this.message.data;
                this.connectionMethods['onConnected'].apply(this);
            }
        }

        this.socket.onclose = (event: CloseEvent) => {
            this.connectionMethods['onDisconnected'].apply(this);
        }

        this.socket.onerror = (event: ErrorEvent) => {
            if(this.enableLogging) {
                console.log('Error data: ' + event.error);
            }
        }
    }

    public invoke(methodName: string, ...args: any[]) {
        let invocationDescriptor = new InvocationDescriptor(methodName, args);
        
        if(this.enableLogging) {
            console.log(invocationDescriptor);
        }

        this.socket.send(JSON.stringify(invocationDescriptor));
    }
}