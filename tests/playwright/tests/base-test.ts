import { test, expect, type Page, type TestInfo } from '@playwright/test';
import path from 'path';
import fs from 'fs';

// Test configuration
const APP_PATH = path.join(__dirname, '../../src/Winhance.WPF/bin/Release/net9.0-windows/Winhance.WPF.exe');
const TEST_DATA_DIR = path.join(__dirname, 'test-data');
const SCREENSHOTS_DIR = path.join(__dirname, 'screenshots');
const PERFORMANCE_LOG = path.join(__dirname, 'performance.json');

// Performance tracking
interface PerformanceMetric {
  feature: string;
  action: string;
  timestamp: number;
  duration: number;
  memory: number;
  cpu: number;
}

// Ensure test directories exist
function ensureDirectories() {
  [TEST_DATA_DIR, SCREENSHOTS_DIR].forEach(dir => {
    if (!fs.existsSync(dir)) {
      fs.mkdirSync(dir, { recursive: true });
    }
  });
}

// Performance logger
function logPerformance(metric: PerformanceMetric) {
  const logs = JSON.parse(fs.readFileSync(PERFORMANCE_LOG, 'utf8') || '[]');
  logs.push(metric);
  fs.writeFileSync(PERFORMANCE_LOG, JSON.stringify(logs, null, 2));
}

// Base test class
export class WinhanceTestBase {
  constructor(public page: Page, public testInfo: TestInfo) {}

  async launchApp() {
    ensureDirectories();
    
    // Launch the WPF application
    await this.page.goto(`file://${APP_PATH}`);
    
    // Wait for app to load
    await this.page.waitForSelector('[data-testid="main-window"]', { timeout: 30000 });
    
    // Maximize window
    await this.page.getByLabel('Maximize').click();
  }

  async measurePerformance<T>(feature: string, action: string, fn: () => Promise<T>): Promise<T> {
    const startTime = Date.now();
    
    // Get initial metrics
    const initialMemory = await this.page.evaluate(() => performance.memory?.usedJSHeapSize || 0);
    
    // Execute action
    const result = await fn();
    
    // Calculate metrics
    const duration = Date.now() - startTime;
    const finalMemory = await this.page.evaluate(() => performance.memory?.usedJSHeapSize || 0);
    
    // Log performance
    logPerformance({
      feature,
      action,
      timestamp: startTime,
      duration,
      memory: finalMemory - initialMemory,
      cpu: 0 // TODO: Implement CPU monitoring
    });
    
    // Assert performance requirements
    expect(duration).toBeLessThan(100); // All actions must complete in <100ms
    
    return result;
  }

  async takeScreenshot(name: string) {
    const screenshotPath = path.join(SCREENSHOTS_DIR, `${name}-${Date.now()}.png`);
    await this.page.screenshot({ path: screenshotPath, fullPage: true });
    return screenshotPath;
  }

  async verifyFeature(featureId: string) {
    // Mark feature as tested
    const testedFeatures = JSON.parse(fs.readFileSync(path.join(__dirname, 'tested-features.json'), 'utf8') || '{}');
    testedFeatures[featureId] = {
      tested: true,
      timestamp: Date.now(),
      testFile: this.testInfo.file,
      testTitle: this.testInfo.title
    };
    fs.writeFileSync(path.join(__dirname, 'tested-features.json'), JSON.stringify(testedFeatures, null, 2));
  }
}

// Custom fixtures
test.extend<{ winhance: WinhanceTestBase }>({
  winhance: async ({ page }, use, testInfo) => {
    const winhance = new WinhanceTestBase(page, testInfo);
    await winhance.launchApp();
    await use(winhance);
  }
});

// Global test configuration
test.beforeAll(async () => {
  ensureDirectories();
  
  // Initialize performance log
  if (!fs.existsSync(PERFORMANCE_LOG)) {
    fs.writeFileSync(PERFORMANCE_LOG, '[]');
  }
  
  // Initialize tested features tracker
  const testedFeaturesPath = path.join(__dirname, 'tested-features.json');
  if (!fs.existsSync(testedFeaturesPath)) {
    fs.writeFileSync(testedFeaturesPath, '{}');
  }
});

test.afterAll(async () => {
  // Generate performance report
  const logs = JSON.parse(fs.readFileSync(PERFORMANCE_LOG, 'utf8'));
  const testedFeatures = JSON.parse(fs.readFileSync(path.join(__dirname, 'tested-features.json'), 'utf8'));
  
  const report = {
    timestamp: new Date().toISOString(),
    totalTests: Object.keys(testedFeatures).length,
    performanceMetrics: logs,
    averageResponseTime: logs.reduce((sum: number, log: PerformanceMetric) => sum + log.duration, 0) / logs.length,
    slowestFeature: logs.reduce((slowest: PerformanceMetric, log: PerformanceMetric) => 
      log.duration > slowest.duration ? log : slowest, logs[0]),
    testedFeatures
  };
  
  fs.writeFileSync(path.join(__dirname, 'test-report.json'), JSON.stringify(report, null, 2));
  
  console.log(`\n=== Test Summary ===`);
  console.log(`Total Features Tested: ${report.totalTests}`);
  console.log(`Average Response Time: ${report.averageResponseTime.toFixed(2)}ms`);
  console.log(`Slowest Feature: ${report.slowestFeature.feature} - ${report.slowestFeature.duration}ms`);
});

// Test utilities
export async function createTestFiles(count: number = 100) {
  const testDir = path.join(TEST_DATA_DIR, 'generated');
  if (!fs.existsSync(testDir)) {
    fs.mkdirSync(testDir, { recursive: true });
  }
  
  for (let i = 0; i < count; i++) {
    const filePath = path.join(testDir, `test-file-${i}.txt`);
    fs.writeFileSync(filePath, `Test file content ${i}`);
  }
  
  return testDir;
}

export async function cleanupTestFiles() {
  const testDir = path.join(TEST_DATA_DIR, 'generated');
  if (fs.existsSync(testDir)) {
    fs.rmSync(testDir, { recursive: true, force: true });
  }
}

// Performance assertions
export function assertPerformance(duration: number, maxMs: number = 100) {
  expect(duration).toBeLessThan(maxMs);
}

// Feature test helpers
export const FEATURE_IDS = {
  // Dual-Pane Browser
  DUAL_PANE: 'DP-001',
  TAB_MANAGEMENT: 'TB-001',
  NAVIGATION: 'NV-001',
  
  // File Operations
  COPY_PASTE: 'FO-001',
  MOVE_FILES: 'FO-002',
  DELETE_FILES: 'FO-003',
  RENAME_FILES: 'FO-004',
  
  // Search & Filter
  SEARCH_BOX: 'SF-001',
  FILTER_RESULTS: 'FT-001',
  
  // Selection
  RUBBER_BAND: 'SEL-001',
  KEYBOARD_SELECT: 'SEL-007',
  
  // Context Menu
  CONTEXT_MENU: 'CTX-001',
  
  // Drag & Drop
  DRAG_DROP: 'DND-001',
  
  // Preview
  IMAGE_PREVIEW: 'IP-001',
  TEXT_PREVIEW: 'DP-003',
  
  // Archives
  ZIP_CREATE: 'AC-001',
  ZIP_EXTRACT: 'AE-001',
  
  // Sync
  FOLDER_SYNC: 'SYNC-001',
  
  // Organization
  AUTO_ORGANIZE: 'ORG-001',
  
  // Space Analysis
  DISK_ANALYSIS: 'SA-001'
};
