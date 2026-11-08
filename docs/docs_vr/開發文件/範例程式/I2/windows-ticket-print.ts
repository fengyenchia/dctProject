import { execFile } from 'node:child_process';
import { promisify } from 'node:util';

// 經 Windows 驅動送印；成功只代表已送到列印系統，仍須工作人員確認出紙與交票。
export async function printTicket(png: string, printer: string): Promise<void> {
  if (process.platform !== 'win32' || !printer) throw new Error('Windows and PRINTER_NAME are required');
  const script = `
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$job = [Text.Encoding]::UTF8.GetString([Convert]::FromBase64String($env:AUDIO_BUGS_PRINT_JOB)) | ConvertFrom-Json
$image = [Drawing.Image]::FromFile($job.png)
$document = [Drawing.Printing.PrintDocument]::new()
try {
  $document.PrinterSettings.PrinterName = $job.printer
  if (-not $document.PrinterSettings.IsValid) { throw 'Printer is not valid' }
  $document.PrintController = [Drawing.Printing.StandardPrintController]::new()
  $document.DocumentName = 'AudioBugs ticket'
  $height = [int][Math]::Ceiling(($image.Height / $image.Width) * 284 + 24)
  $document.DefaultPageSettings.PaperSize = [Drawing.Printing.PaperSize]::new('AudioBugs 80mm',315,$height)
  $document.DefaultPageSettings.Margins = [Drawing.Printing.Margins]::new(0,0,0,0)
  $handler = [Drawing.Printing.PrintPageEventHandler] {
    param($sender,$printEvent)
    $width = [Math]::Min(284, $printEvent.PageBounds.Width)
    $printEvent.Graphics.DrawImage($image,0,0,$width,[single]($width * $image.Height / $image.Width))
    $printEvent.HasMorePages = $false
  }
  $document.add_PrintPage($handler)
  $document.Print()
} finally {
  $document.Dispose()
  $image.Dispose()
}`;
  // 設定透過環境變數傳入，不把檔名或印表機名稱拼成 PowerShell 指令。
  await promisify(execFile)('powershell.exe', ['-NoProfile', '-NonInteractive', '-EncodedCommand', Buffer.from(script, 'utf16le').toString('base64')], {
    windowsHide: true, timeout: 30000,
    env: { ...process.env, AUDIO_BUGS_PRINT_JOB: Buffer.from(JSON.stringify({ png, printer })).toString('base64') },
  });
}
