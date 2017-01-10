export class InvocationDescriptor {
    public methodName: string;
    public arguments: Array<any>;

    constructor(methodName: string, args: any[]) {
        this.methodName = methodName;
        this.arguments = args;
    }
}