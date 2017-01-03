/* global describe, it, beforeEach */
'use strict';
var assert  = require('assert');


/**
 * Test App.js
 */
describe('typescript greeter', function () {
  var self = this;

  /**
   * BeforeEach: Create greeter with view to catch messages.
   */
  beforeEach(function () {
    self.result = '';

    self.testView = {
      display : function (msg) {
        self.result = msg;
      }
    };

    self.App = require('../app/build/app.js');
  });

  /**
   * Test 1: Default greeter works.
   */
  it('constructs a hello world app', function () {
    var expected = 'Hello, world!';
    var app = self.App.defaultGreeter(self.testView);

    app.greet();
    assert.equal(expected, self.result);
  });

  /**
   * Test 2: Create generator with default settings.
   */
  it('greets as expected', function () {
    var expected = 'Whatup, world!';
    var app = new self.App.Controller("Whatup", self.testView);

    app.greet();
    assert.equal(expected, self.result);
  });
});