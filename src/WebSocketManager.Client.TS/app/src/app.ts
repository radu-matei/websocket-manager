/**
 * An output capable of recieving a string message for display.
 */
export interface StringDisplay {
  display(msg : string) : void;
}

/**
 * Instance is an App controller. Automatically creates 
 * model. Creates view if none given.
 */
export class Controller {
  private model : Model;
  private view  : View ;

  constructor (greeting: string, view?: View) {
    this.model = new Model(greeting);
    this.view  = (view  || new View());
  }

  public greet () : void {
    this.view.display (this.model.getGreeting());
  }
}

/**
 * Private class. Instance represents a greeting to the world.
 */
class Model {
  private greeting : string;

  constructor (greeting : string) {
    this.greeting = greeting;
  }

  public getGreeting() : string {
    return this.greeting + ", world!";
  }
}

/**
 * Instance is a message logger; outputs messages to console.
 */
export class View implements StringDisplay {
  public display (msg : string) : void {
    console.log(msg);
  }
}

/*
 * Factory function. Returns a default first app.
 */
export function defaultGreeter(view? : StringDisplay) {
  return new Controller("Hello", view);
}
