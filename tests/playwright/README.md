# Playwright Testing Setup

## Installation

```bash
cd tests/playwright
npm install
npm run install
```

## Running Tests

```bash
# Run all tests
npm test

# Run tests in headed mode
npm run test:headed

# Run tests with UI
npm run test:ui

# Run performance tests
npm run test:performance

# Run smoke tests
npm run test:smoke
```

## Test Structure

- `tests/base-test.ts` - Base test class with utilities
- `tests/file-manager.spec.ts` - GUI feature tests
- `tests/cli.spec.ts` - CLI command tests
- `test-data/` - Test data directory
- `screenshots/` - Test screenshots
- `performance.json` - Performance metrics

## Notes

The test files require Node.js dependencies to be installed before they can be run without TypeScript errors. The errors shown in the IDE are expected until `npm install` is run in the tests/playwright directory.
