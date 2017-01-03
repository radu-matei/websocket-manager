/* global describe, it, beforeEach */
'use strict';
var assert = require('assert');


describe('typescript greeter', function () {
	/**
	 * Test 1: Project is successfully compiled, and can be imported.
	 */
	it('can be imported without blowing up', function () {
	  assert(require('../app/build/app.js') !== undefined);
	});
});