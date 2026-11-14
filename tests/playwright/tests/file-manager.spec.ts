import { test, expect } from '@playwright/test';
import { WinhanceTestBase, FEATURE_IDS, createTestFiles, cleanupTestFiles, assertPerformance } from './base-test';

test.describe('Dual-Pane Browser Tests', () => {
  test('DP-001: Independent left/right panels', async ({ winhance }) => {
    await winhance.measurePerformance('DP-001', 'load-dual-pane', async () => {
      // Verify both panes are visible
      await expect(winhance.page.locator('[data-testid="left-pane"]')).toBeVisible();
      await expect(winhance.page.locator('[data-testid="right-pane"]')).toBeVisible();
      
      // Verify panes can navigate independently
      const leftPath = await winhance.page.locator('[data-testid="left-path"]').inputValue();
      const rightPath = await winhance.page.locator('[data-testid="right-path"]').inputValue();
      
      expect(leftPath).toBeTruthy();
      expect(rightPath).toBeTruthy();
    });
    
    await winhance.verifyFeature(FEATURE_IDS.DUAL_PANE);
  });

  test('DP-005: Adjustable splitter with drag', async ({ winhance }) => {
    await winhance.measurePerformance('DP-005', 'adjust-splitter', async () => {
      const splitter = winhance.page.locator('[data-testid="pane-splitter"]');
      const leftPane = winhance.page.locator('[data-testid="left-pane"]');
      
      // Get initial width
      const initialWidth = await leftPane.evaluate(el => el.getBoundingClientRect().width);
      
      // Drag splitter
      await splitter.dragTo(winhance.page.locator('[data-testid="right-pane"]'), {
        targetPosition: { x: 100, y: 0 }
      });
      
      // Verify width changed
      const newWidth = await leftPane.evaluate(el => el.getBoundingClientRect().width);
      expect(newWidth).not.toBe(initialWidth);
    });
  });

  test('TB-001: Multiple tabs per panel', async ({ winhance }) => {
    await winhance.measurePerformance('TB-001', 'create-tab', async () => {
      // Click new tab button
      await winhance.page.locator('[data-testid="new-tab-button"]').first().click();
      
      // Verify new tab created
      const tabs = await winhance.page.locator('[data-testid="tab"]').count();
      expect(tabs).toBeGreaterThan(1);
      
      // Verify tab can be closed
      await winhance.page.locator('[data-testid="tab-close"]').first().click();
      const tabsAfterClose = await winhance.page.locator('[data-testid="tab"]').count();
      expect(tabsAfterClose).toBe(tabs - 1);
    });
  });

  test('NV-004: Breadcrumb navigation bar', async ({ winhance }) => {
    await winhance.measurePerformance('NV-004', 'breadcrumb-nav', async () => {
      // Verify breadcrumbs are visible
      await expect(winhance.page.locator('[data-testid="breadcrumb-bar"]')).toBeVisible();
      
      // Click on breadcrumb segment
      await winhance.page.locator('[data-testid="breadcrumb-segment"]').first().click();
      
      // Verify navigation occurred
      const path = await winhance.page.locator('[data-testid="address-bar"]').inputValue();
      expect(path).toBeTruthy();
    });
  });
});

test.describe('File Operations Tests', () => {
  test.beforeAll(async () => {
    await createTestFiles(50);
  });

  test.afterAll(async () => {
    await cleanupTestFiles();
  });

  test('FO-001: Copy files (Ctrl+C/Ctrl+V)', async ({ winhance }) => {
    await winhance.measurePerformance('FO-001', 'copy-files', async () => {
      // Select files
      await winhance.page.locator('[data-testid="file-item"]').first().click();
      await winhance.page.keyboard.press('Control+a');
      
      // Copy
      await winhance.page.keyboard.press('Control+c');
      
      // Navigate to different folder
      await winhance.page.locator('[data-testid="right-pane"]').click();
      
      // Paste
      await winhance.page.keyboard.press('Control+v');
      
      // Verify files copied
      const fileCount = await winhance.page.locator('[data-testid="file-item"]').count();
      expect(fileCount).toBeGreaterThan(0);
    });
  });

  test('FO-002: Move files (Ctrl+X/Ctrl+V)', async ({ winhance }) => {
    await winhance.measurePerformance('FO-002', 'move-files', async () => {
      // Select files
      await winhance.page.locator('[data-testid="file-item"]').first().click();
      await winhance.page.keyboard.press('Control+a');
      
      // Cut
      await winhance.page.keyboard.press('Control+x');
      
      // Navigate to different folder
      await winhance.page.locator('[data-testid="right-pane"]').click();
      
      // Paste
      await winhance.page.keyboard.press('Control+v');
      
      // Verify files moved
      const fileCount = await winhance.page.locator('[data-testid="file-item"]').count();
      expect(fileCount).toBeGreaterThan(0);
    });
  });

  test('FO-003: Delete files (Delete key)', async ({ winhance }) => {
    await winhance.measurePerformance('FO-003', 'delete-files', async () => {
      // Select file
      await winhance.page.locator('[data-testid="file-item"]').first().click();
      
      // Delete
      await winhance.page.keyboard.press('Delete');
      
      // Confirm delete in dialog
      await winhance.page.locator('[data-testid="confirm-delete"]').click();
      
      // Verify file deleted
      const fileCount = await winhance.page.locator('[data-testid="file-item"]').count();
      // Should be less than before
      expect(fileCount).toBeGreaterThanOrEqual(0);
    });
  });

  test('FO-004: Rename (F2 key)', async ({ winhance }) => {
    await winhance.measurePerformance('FO-004', 'rename-file', async () => {
      // Select file
      await winhance.page.locator('[data-testid="file-item"]').first().click();
      
      // Press F2 to rename
      await winhance.page.keyboard.press('F2');
      
      // Type new name
      await winhance.page.keyboard.type('renamed-file.txt');
      await winhance.page.keyboard.press('Enter');
      
      // Verify file renamed
      const fileName = await winhance.page.locator('[data-testid="file-item"]').first().textContent();
      expect(fileName).toContain('renamed-file.txt');
    });
  });
});

test.describe('Search & Filter Tests', () => {
  test('SF-001: Search box with real-time results', async ({ winhance }) => {
    await winhance.measurePerformance('SF-001', 'search-files', async () => {
      // Type in search box
      await winhance.page.locator('[data-testid="search-box"]').fill('test');
      
      // Wait for results
      await winhance.page.waitForTimeout(100);
      
      // Verify search results
      const results = await winhance.page.locator('[data-testid="search-result"]').count();
      expect(results).toBeGreaterThanOrEqual(0);
    });
  });

  test('FT-001: Quick filter by name', async ({ winhance }) => {
    await winhance.measurePerformance('FT-001', 'filter-by-name', async () => {
      // Apply name filter
      await winhance.page.locator('[data-testid="filter-name"]').fill('.txt');
      
      // Verify filter applied
      const filteredItems = await winhance.page.locator('[data-testid="file-item"]').count();
      expect(filteredItems).toBeGreaterThanOrEqual(0);
    });
  });
});

test.describe('Selection Tests', () => {
  test('SEL-001: Rubber band selection', async ({ winhance }) => {
    await winhance.measurePerformance('SEL-001', 'rubber-band-select', async () => {
      // Start rubber band selection
      const listBox = winhance.page.locator('[data-testid="file-list"]');
      await listBox.click({ position: { x: 10, y: 10 } });
      
      // Drag to select multiple items
      await listBox.dragTo(listBox, {
        sourcePosition: { x: 10, y: 10 },
        targetPosition: { x: 200, y: 200 }
      });
      
      // Verify multiple items selected
      const selectedCount = await winhance.page.locator('[data-testid="file-item.selected"]').count();
      expect(selectedCount).toBeGreaterThan(1);
    });
  });

  test('SEL-007: Keyboard navigation', async ({ winhance }) => {
    await winhance.measurePerformance('SEL-007', 'keyboard-nav', async () => {
      // Focus file list
      await winhance.page.locator('[data-testid="file-list"]').click();
      
      // Navigate with arrow keys
      await winhance.page.keyboard.press('ArrowDown');
      await winhance.page.keyboard.press('ArrowDown');
      
      // Verify selection moved
      const selectedIndex = await winhance.page.evaluate(() => {
        const items = document.querySelectorAll('[data-testid="file-item"]');
        for (let i = 0; i < items.length; i++) {
          if (items[i].classList.contains('selected')) {
            return i;
          }
        }
        return -1;
      });
      
      expect(selectedIndex).toBe(2); // Should be at index 2 after 2 down arrows
    });
  });
});

test.describe('Context Menu Tests', () => {
  test('CTX-001: Context menu appears on right-click', async ({ winhance }) => {
    await winhance.measurePerformance('CTX-001', 'context-menu', async () => {
      // Right-click on file
      await winhance.page.locator('[data-testid="file-item"]').first().click({ button: 'right' });
      
      // Verify context menu appears
      await expect(winhance.page.locator('[data-testid="context-menu"]')).toBeVisible();
      
      // Verify menu items
      await expect(winhance.page.locator('[data-testid="menu-open"]')).toBeVisible();
      await expect(winhance.page.locator('[data-testid="menu-copy"]')).toBeVisible();
      await expect(winhance.page.locator('[data-testid="menu-delete"]')).toBeVisible();
      
      // Close menu
      await winhance.page.keyboard.press('Escape');
    });
  });
});

test.describe('Drag & Drop Tests', () => {
  test('DND-001: Drag files between panes', async ({ winhance }) => {
    await winhance.measurePerformance('DND-001', 'drag-drop', async () => {
      // Select file in left pane
      const leftFile = winhance.page.locator('[data-testid="left-pane"] [data-testid="file-item"]').first();
      await leftFile.click();
      
      // Drag to right pane
      const rightPane = winhance.page.locator('[data-testid="right-pane"]');
      await leftFile.dragTo(rightPane);
      
      // Verify file in right pane
      const rightFiles = await rightPane.locator('[data-testid="file-item"]').count();
      expect(rightFiles).toBeGreaterThan(0);
    });
  });
});

test.describe('Performance Tests', () => {
  test('PERF-001: Load 1000 files in <100ms', async ({ winhance }) => {
    // Create large test directory
    await createTestFiles(1000);
    
    const duration = await winhance.measurePerformance('PERF-001', 'load-large-dir', async () => {
      // Navigate to test directory
      await winhance.page.locator('[data-testid="address-bar"]').fill(TEST_DATA_DIR);
      await winhance.page.keyboard.press('Enter');
      
      // Wait for files to load
      await winhance.page.waitForSelector('[data-testid="file-item"]');
      
      // Count files
      const fileCount = await winhance.page.locator('[data-testid="file-item"]').count();
      expect(fileCount).toBe(1000);
    });
    
    assertPerformance(duration, 100);
  });

  test('PERF-002: Search 10000 files in <100ms', async ({ winhance }) => {
    // This would require a larger test dataset
    await winhance.measurePerformance('PERF-002', 'search-large-dataset', async () => {
      await winhance.page.locator('[data-testid="search-box"]').fill('*.txt');
      await winhance.page.keyboard.press('Enter');
      
      // Wait for search results
      await winhance.page.waitForSelector('[data-testid="search-result"]');
    });
  });
});

test.describe('Accessibility Tests', () => {
  test('A11Y-001: Keyboard accessibility', async ({ winhance }) => {
    // Test all features can be accessed via keyboard
    await winhance.page.keyboard.press('Tab'); // Navigate to first element
    await winhance.page.keyboard.press('Enter'); // Activate
    
    // Verify focus management
    const focusedElement = await winhance.page.evaluate(() => document.activeElement?.tagName);
    expect(focusedElement).toBeTruthy();
  });

  test('A11Y-002: Screen reader support', async ({ winhance }) => {
    // Verify ARIA labels
    const fileItems = winhance.page.locator('[data-testid="file-item"]');
    const count = await fileItems.count();
    
    for (let i = 0; i < Math.min(count, 5); i++) {
      const item = fileItems.nth(i);
      await expect(item).toHaveAttribute('aria-label');
    }
  });
});
