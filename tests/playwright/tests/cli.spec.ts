import { test, expect } from '@playwright/test';
import { execSync } from 'child_process';
import path from 'path';
import fs from 'fs';

const CLI_PATH = path.join(__dirname, '../../../src/nexus_cli/__main__.py');
const TEST_DATA_DIR = path.join(__dirname, 'test-data');

test.describe('CLI Commands Tests', () => {
  test.beforeAll(async () => {
    // Create test data
    if (!fs.existsSync(TEST_DATA_DIR)) {
      fs.mkdirSync(TEST_DATA_DIR, { recursive: true });
    }
    
    // Create test files
    for (let i = 0; i < 10; i++) {
      fs.writeFileSync(path.join(TEST_DATA_DIR, `test${i}.txt`), `Test file ${i}`);
    }
    
    fs.mkdirSync(path.join(TEST_DATA_DIR, 'subdir'));
    fs.writeFileSync(path.join(TEST_DATA_DIR, 'subdir', 'nested.txt'), 'Nested file');
  });

  test.afterAll(async () => {
    // Cleanup
    fs.rmSync(TEST_DATA_DIR, { recursive: true, force: true });
  });

  function runCLI(args: string[]): { stdout: string; stderr: string; exitCode: number } {
    try {
      const stdout = execSync(`python "${CLI_PATH}" ${args.join(' ')}`, { 
        encoding: 'utf8',
        cwd: TEST_DATA_DIR 
      });
      return { stdout, stderr: '', exitCode: 0 };
    } catch (error: any) {
      return {
        stdout: error.stdout,
        stderr: error.stderr,
        exitCode: error.status
      };
    }
  }

  test('CLI-001: nexus ls - List directory', async () => {
    const result = runCLI(['ls']);
    
    expect(result.exitCode).toBe(0);
    expect(result.stdout).toContain('test0.txt');
    expect(result.stdout).toContain('subdir');
  });

  test('CLI-002: nexus ls -l - Long format', async () => {
    const result = runCLI(['ls', '-l']);
    
    expect(result.exitCode).toBe(0);
    expect(result.stdout).toMatch(/\d+\s+B/); // Should show file sizes
  });

  test('CLI-003: nexus cd - Change directory', async () => {
    const result = runCLI(['cd', 'subdir']);
    
    expect(result.exitCode).toBe(0);
    // CD command might not produce output but should not error
  });

  test('CLI-004: nexus cp - Copy files', async () => {
    // First create a destination directory
    fs.mkdirSync(path.join(TEST_DATA_DIR, 'dest'));
    
    const result = runCLI(['cp', 'test0.txt', 'dest/']);
    
    expect(result.exitCode).toBe(0);
    expect(fs.existsSync(path.join(TEST_DATA_DIR, 'dest', 'test0.txt'))).toBe(true);
  });

  test('CLI-005: nexus mv - Move files', async () => {
    const result = runCLI(['mv', 'test1.txt', 'moved.txt']);
    
    expect(result.exitCode).toBe(0);
    expect(fs.existsSync(path.join(TEST_DATA_DIR, 'moved.txt'))).toBe(true);
    expect(fs.existsSync(path.join(TEST_DATA_DIR, 'test1.txt'))).toBe(false);
  });

  test('CLI-006: nexus rm - Delete files', async () => {
    const result = runCLI(['rm', 'test2.txt']);
    
    expect(result.exitCode).toBe(0);
    expect(fs.existsSync(path.join(TEST_DATA_DIR, 'test2.txt'))).toBe(false);
  });

  test('CLI-007: nexus mkdir - Create directory', async () => {
    const result = runCLI(['mkdir', 'newdir']);
    
    expect(result.exitCode).toBe(0);
    expect(fs.existsSync(path.join(TEST_DATA_DIR, 'newdir'))).toBe(true);
  });

  test('CLI-008: nexus search - Search files', async () => {
    const result = runCLI(['search', '*.txt']);
    
    expect(result.exitCode).toBe(0);
    expect(result.stdout).toContain('.txt');
  });

  test('CLI-009: nexus find - Find files', async () => {
    const result = runCLI(['find', '-name', 'test*.txt']);
    
    expect(result.exitCode).toBe(0);
    expect(result.stdout).toContain('test');
  });

  test('CLI-010: nexus space - Analyze space', async () => {
    const result = runCLI(['space', '.']);
    
    expect(result.exitCode).toBe(0);
    expect(result.stdout).toContain('Total');
  });

  test('CLI-011: nexus tree - Directory tree', async () => {
    const result = runCLI(['tree']);
    
    expect(result.exitCode).toBe(0);
    expect(result.stdout).toContain('├──');
    expect(result.stdout).toContain('test0.txt');
  });

  test('CLI-012: nexus info - File info', async () => {
    const result = runCLI(['info', 'test0.txt']);
    
    expect(result.exitCode).toBe(0);
    expect(result.stdout).toContain('Size:');
    expect(result.stdout).toContain('Modified:');
  });

  test('CLI-013: Performance test - All commands under 100ms', async () => {
    const commands = [
      ['ls'],
      ['ls', '-l'],
      ['cd', '.'],
      ['mkdir', 'perf-test'],
      ['search', '*.txt'],
      ['find', '-name', '*.txt'],
      ['tree'],
      ['info', 'test0.txt']
    ];
    
    for (const cmd of commands) {
      const start = Date.now();
      const result = runCLI(cmd);
      const duration = Date.now() - start;
      
      expect(result.exitCode).toBe(0);
      expect(duration).toBeLessThan(100, `Command ${cmd.join(' ')} took ${duration}ms`);
    }
  });
});

test.describe('CLI Integration Tests', () => {
  test('CLI-INT-001: Complex workflow - Copy, Move, Delete', async () => {
    const result1 = runCLI(['cp', 'test3.txt', 'backup.txt']);
    expect(result1.exitCode).toBe(0);
    
    const result2 = runCLI(['mv', 'backup.txt', 'moved_backup.txt']);
    expect(result2.exitCode).toBe(0);
    
    const result3 = runCLI(['rm', 'moved_backup.txt']);
    expect(result3.exitCode).toBe(0);
  });

  test('CLI-INT-002: Error handling - Invalid paths', async () => {
    const result = runCLI(['cd', '/nonexistent/path']);
    
    expect(result.exitCode).not.toBe(0);
    expect(result.stderr).toContain('not found');
  });

  test('CLI-INT-003: Help command', async () => {
    const result = runCLI(['--help']);
    
    expect(result.exitCode).toBe(0);
    expect(result.stdout).toContain('Usage:');
  });
});
