using System.ComponentModel;
using System.Diagnostics;
using System.Text;

namespace FemScanner.NastranRunner;

/// <summary>
/// 외부 Nastran 프로세스를 실행하고 F06 파일 생성을 대기하는 래퍼.
/// --nastran 옵션 없을 시 절대 실행하지 않습니다.
/// Nastran은 시스템 PATH에 등록되어 있다고 가정합니다.
/// </summary>
public class NastranRunner
{
    /// <summary>
    /// Nastran을 실행하고 생성된 F06 파일 경로를 반환합니다.
    /// 실패 시 stdout/stderr 내용을 예외 메시지에 포함합니다.
    /// </summary>
    /// <param name="bdfPath">BDF 파일 경로</param>
    /// <param name="timeoutSec">타임아웃(초, 기본 300)</param>
    /// <returns>생성된 F06 파일 경로</returns>
    /// <exception cref="InvalidOperationException">nastran 명령을 찾을 수 없을 때</exception>
    /// <exception cref="FileNotFoundException">F06 파일이 생성되지 않았을 때</exception>
    /// <exception cref="TimeoutException">타임아웃 초과 시</exception>
    public string Run(string bdfPath, int timeoutSec = 300)
    {
        string bdfFullPath = Path.GetFullPath(bdfPath);
        var psi = new ProcessStartInfo
        {
            FileName = "nastran",
            Arguments = $"\"{bdfFullPath}\"",
            WorkingDirectory = Path.GetDirectoryName(bdfFullPath)!,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        var stdout = new StringBuilder();
        var stderr = new StringBuilder();

        Process process;
        try
        {
            process = Process.Start(psi)
                ?? throw new InvalidOperationException("Nastran 프로세스를 시작할 수 없습니다.");
        }
        catch (Win32Exception)
        {
            throw new InvalidOperationException(
                "nastran 명령을 찾을 수 없습니다. Nastran이 시스템 PATH에 등록되어 있는지 확인하세요.");
        }

        using (process)
        {
            // stdout/stderr를 비동기로 수집 (데드락 방지)
            process.OutputDataReceived += (_, e) => { if (e.Data != null) stdout.AppendLine(e.Data); };
            process.ErrorDataReceived += (_, e) => { if (e.Data != null) stderr.AppendLine(e.Data); };
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            bool exited = process.WaitForExit(timeoutSec * 1000);
            if (!exited)
            {
                process.Kill(entireProcessTree: true);
                throw new TimeoutException(
                    $"Nastran 실행이 {timeoutSec}초 내에 완료되지 않았습니다.\n" +
                    BuildOutputSummary(stdout, stderr));
            }
        }

        string f06Path = Path.ChangeExtension(bdfFullPath, ".f06");
        if (!File.Exists(f06Path))
        {
            throw new FileNotFoundException(
                $"Nastran 실행 후 F06 파일이 생성되지 않았습니다: {f06Path}\n" +
                BuildOutputSummary(stdout, stderr));
        }

        return f06Path;
    }

    private static string BuildOutputSummary(StringBuilder stdout, StringBuilder stderr)
    {
        var sb = new StringBuilder();

        string stdoutStr = stdout.ToString().Trim();
        if (!string.IsNullOrEmpty(stdoutStr))
        {
            sb.AppendLine("[Nastran stdout]");
            sb.AppendLine(stdoutStr);
        }

        string stderrStr = stderr.ToString().Trim();
        if (!string.IsNullOrEmpty(stderrStr))
        {
            sb.AppendLine("[Nastran stderr]");
            sb.AppendLine(stderrStr);
        }

        if (sb.Length == 0)
            sb.AppendLine("[Nastran stdout/stderr 출력 없음]");

        return sb.ToString().TrimEnd();
    }
}
