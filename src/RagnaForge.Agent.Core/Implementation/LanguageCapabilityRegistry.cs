using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace RagnaForge.Agent.Core.Implementation;

public sealed class LanguageCapability
{
    public string Key { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public IReadOnlyCollection<string> FileExtensions { get; init; } = [];
    public Func<ProjectLanguageContext, bool>? ProjectDetector { get; init; }
    public Func<string, string, LanguageValidationResult> Validator { get; init; } = (_, content) =>
        new LanguageValidationResult { Valid = true, FormattedContent = content };
    public Func<string, string, string> Formatter { get; init; } = (path, content) =>
        LanguageCapabilityRegistry.NormalizeText(content, LanguageCapabilityRegistry.SuggestedLineEnding(path, content));
    public Func<LanguageScaffoldRequest, string> ScaffoldGenerator { get; init; } = request =>
        request.Description ?? request.Title ?? string.Empty;
}

public sealed class LanguageCapabilityRegistry
{
    private readonly IReadOnlyList<LanguageCapability> _capabilities;

    public LanguageCapabilityRegistry()
    {
        _capabilities = BuildCapabilities();
    }

    public IReadOnlyList<LanguageCapability> All => _capabilities;

    public LanguageCapability? ResolveByPath(string targetPath, string? explicitLanguage = null)
    {
        if (!string.IsNullOrWhiteSpace(explicitLanguage))
        {
            var named = _capabilities.FirstOrDefault(cap =>
                cap.Key.Equals(explicitLanguage, StringComparison.OrdinalIgnoreCase) ||
                cap.DisplayName.Equals(explicitLanguage, StringComparison.OrdinalIgnoreCase));
            if (named is not null)
                return named;
        }

        var extension = Path.GetExtension(targetPath);
        if (string.IsNullOrWhiteSpace(extension))
            return null;

        return _capabilities.FirstOrDefault(cap =>
            cap.FileExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase));
    }

    public IReadOnlyList<string> DetectProjectEcosystems(string rootPath)
    {
        var context = BuildProjectContext(rootPath);
        return _capabilities
            .Where(cap => cap.ProjectDetector?.Invoke(context) == true)
            .Select(cap => cap.Key)
            .OrderBy(key => key, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public ProjectLanguageContext BuildProjectContext(string rootPath)
    {
        var fullRoot = Path.GetFullPath(rootPath);
        var context = new ProjectLanguageContext { RootPath = fullRoot };

        var packageJson = Path.Combine(fullRoot, "package.json");
        if (File.Exists(packageJson))
            context.PackageJsonContent = File.ReadAllText(packageJson);

        var composerJson = Path.Combine(fullRoot, "composer.json");
        if (File.Exists(composerJson))
            context.ComposerJsonContent = File.ReadAllText(composerJson);

        context.HasPomXml = File.Exists(Path.Combine(fullRoot, "pom.xml"));
        context.HasGradleBuild = File.Exists(Path.Combine(fullRoot, "build.gradle")) || File.Exists(Path.Combine(fullRoot, "build.gradle.kts"));
        context.HasMakefile = File.Exists(Path.Combine(fullRoot, "Makefile"));
        context.HasCMakeLists = File.Exists(Path.Combine(fullRoot, "CMakeLists.txt"));
        context.HasPyProject = File.Exists(Path.Combine(fullRoot, "pyproject.toml"));
        context.HasRequirementsTxt = File.Exists(Path.Combine(fullRoot, "requirements.txt"));
        context.HasShellScripts = Directory.Exists(fullRoot) && Directory.EnumerateFiles(fullRoot, "*.sh", SearchOption.AllDirectories).Take(1).Any();
        context.HasDotnetSolution = Directory.Exists(fullRoot) && Directory.EnumerateFiles(fullRoot, "*.sln*", SearchOption.TopDirectoryOnly).Take(1).Any();
        context.HasLuaFiles = Directory.Exists(fullRoot) && Directory.EnumerateFiles(fullRoot, "*.lua", SearchOption.AllDirectories).Take(1).Any();
        context.SampleFiles = Directory.Exists(fullRoot)
            ? Directory.EnumerateFiles(fullRoot, "*.*", SearchOption.AllDirectories).Take(50).ToList()
            : [];
        return context;
    }

    private static IReadOnlyList<LanguageCapability> BuildCapabilities()
    {
        return
        [
            BuildHtmlCapability(),
            BuildCssCapability(),
            BuildBootstrapCapability(),
            BuildPhpCapability(),
            BuildJavaCapability(),
            BuildJavaScriptCapability(),
            BuildNodeCapability(),
            BuildShellCapability(),
            BuildCCapability(),
            BuildCppCapability(),
            BuildCSharpCapability(),
            BuildPythonCapability(),
            BuildLuaCapability(),
            BuildPowerShellCapability()
        ];
    }

    private static LanguageCapability BuildHtmlCapability() => new()
    {
        Key = "html",
        DisplayName = "HTML",
        FileExtensions = [".html", ".htm"],
        Validator = (path, content) =>
        {
            var result = CreateValidationResult(path, content);
            if (!content.Contains('<'))
            {
                result.Valid = false;
                result.Messages.Add(Error("html.missing_markup", "HTML content must contain markup."));
            }
            if (Count(content, '<') != Count(content, '>'))
            {
                result.Valid = false;
                result.Messages.Add(Error("html.unbalanced_markup", "HTML tag delimiters appear unbalanced."));
            }
            return result;
        },
        ScaffoldGenerator = request =>
        {
            var title = request.Title ?? request.Name ?? "Agente Setimmo";
            var description = request.Description ?? "Generated by Agente Setimmo.";
            return $$"""
            <!DOCTYPE html>
            <html lang="en">
            <head>
              <meta charset="utf-8">
              <meta name="viewport" content="width=device-width, initial-scale=1">
              <title>{{title}}</title>
            </head>
            <body>
              <main>
                <h1>{{title}}</h1>
                <p>{{description}}</p>
              </main>
            </body>
            </html>
            """;
        }
    };

    private static LanguageCapability BuildCssCapability() => new()
    {
        Key = "css",
        DisplayName = "CSS",
        FileExtensions = [".css", ".scss", ".sass"],
        Validator = (path, content) =>
        {
            var result = CreateValidationResult(path, content);
            if (!HasBalancedPairs(content, '{', '}'))
            {
                result.Valid = false;
                result.Messages.Add(Error("css.unbalanced_braces", "CSS braces are unbalanced."));
            }
            return result;
        },
        ScaffoldGenerator = request =>
        {
            var selector = request.Name ?? "body";
            var description = request.Description ?? "Generated by Agente Setimmo.";
            return $$"""
            /* {{description}} */
            {{selector}} {
              margin: 0;
              font-family: sans-serif;
            }
            """;
        }
    };

    private static LanguageCapability BuildBootstrapCapability() => new()
    {
        Key = "bootstrap",
        DisplayName = "Bootstrap",
        FileExtensions = [".html", ".htm"],
        ProjectDetector = context =>
            (context.PackageJsonContent?.Contains("\"bootstrap\"", StringComparison.OrdinalIgnoreCase) ?? false) ||
            context.SampleFiles.Any(file => file.EndsWith(".html", StringComparison.OrdinalIgnoreCase) &&
                                           File.ReadAllText(file).Contains("bootstrap", StringComparison.OrdinalIgnoreCase)),
        Validator = (path, content) =>
        {
            var result = CreateValidationResult(path, content);
            if (!content.Contains("container", StringComparison.OrdinalIgnoreCase))
            {
                result.Messages.Add(Warning("bootstrap.no_container", "Bootstrap component has no container/class-based layout hint."));
            }
            return result;
        },
        ScaffoldGenerator = request =>
        {
            var title = request.Title ?? request.Name ?? "Bootstrap Component";
            var description = request.Description ?? "Generated by Agente Setimmo.";
            return $$"""
            <section class="container py-4">
              <div class="card shadow-sm">
                <div class="card-body">
                  <h1 class="h4 mb-2">{{title}}</h1>
                  <p class="mb-0 text-body-secondary">{{description}}</p>
                </div>
              </div>
            </section>
            """;
        }
    };

    private static LanguageCapability BuildPhpCapability() => new()
    {
        Key = "php",
        DisplayName = "PHP",
        FileExtensions = [".php"],
        ProjectDetector = context => !string.IsNullOrWhiteSpace(context.ComposerJsonContent),
        Validator = (path, content) =>
        {
            var result = CreateValidationResult(path, content);
            if (!content.Contains("<?php", StringComparison.Ordinal))
            {
                result.Valid = false;
                result.Messages.Add(Error("php.missing_open_tag", "PHP content must start with <?php."));
            }
            if (!HasBalancedPairs(content, '{', '}'))
            {
                result.Valid = false;
                result.Messages.Add(Error("php.unbalanced_braces", "PHP braces are unbalanced."));
            }
            return result;
        },
        ScaffoldGenerator = request =>
        {
            var name = request.Name ?? "GeneratedHandler";
            return $$"""
            <?php

            final class {{name}}
            {
                public static function handle(): string
                {
                    return 'Generated by Agente Setimmo';
                }
            }
            """;
        }
    };

    private static LanguageCapability BuildJavaCapability() => new()
    {
        Key = "java",
        DisplayName = "Java",
        FileExtensions = [".java"],
        ProjectDetector = context => context.HasPomXml || context.HasGradleBuild,
        Validator = (path, content) =>
        {
            var result = CreateValidationResult(path, content);
            if (!content.Contains("class ", StringComparison.Ordinal))
            {
                result.Valid = false;
                result.Messages.Add(Error("java.missing_class", "Java file must declare a class."));
            }
            if (!HasBalancedPairs(content, '{', '}'))
            {
                result.Valid = false;
                result.Messages.Add(Error("java.unbalanced_braces", "Java braces are unbalanced."));
            }
            return result;
        },
        ScaffoldGenerator = request =>
        {
            var name = request.Name ?? "GeneratedClass";
            return $$"""
            public final class {{name}} {
                public String describe() {
                    return "Generated by Agente Setimmo";
                }
            }
            """;
        }
    };

    private static LanguageCapability BuildJavaScriptCapability() => new()
    {
        Key = "javascript",
        DisplayName = "JavaScript",
        FileExtensions = [".js", ".mjs", ".cjs", ".jsx", ".ts", ".tsx"],
        ProjectDetector = context => !string.IsNullOrWhiteSpace(context.PackageJsonContent),
        Validator = (path, content) =>
        {
            var result = CreateValidationResult(path, content);
            if (!HasBalancedPairs(content, '{', '}'))
            {
                result.Valid = false;
                result.Messages.Add(Error("javascript.unbalanced_braces", "JavaScript braces are unbalanced."));
            }
            if (Regex.IsMatch(content, @"\b(child_process|execSync|spawnSync)\b|\.exec\s*\(|\beval\s*\(", RegexOptions.IgnoreCase))
            {
                result.Valid = false;
                result.Messages.Add(Error("javascript.generic_shell_or_eval", "JavaScript content exposes shell execution or eval-like behavior."));
            }
            return result;
        },
        ScaffoldGenerator = request =>
        {
            var name = ToIdentifier(request.Name ?? "generatedUtility");
            return $$"""
            export function {{name}}() {
              return "Generated by Agente Setimmo";
            }
            """;
        }
    };

    private static LanguageCapability BuildNodeCapability() => new()
    {
        Key = "node",
        DisplayName = "Node.js",
        FileExtensions = [".js", ".mjs", ".cjs", ".json"],
        ProjectDetector = context => !string.IsNullOrWhiteSpace(context.PackageJsonContent),
        Validator = (path, content) =>
        {
            var result = CreateValidationResult(path, content);
            if (Path.GetFileName(path).Equals("package.json", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    JsonDocument.Parse(content);
                }
                catch (JsonException ex)
                {
                    result.Valid = false;
                    result.Messages.Add(Error("node.invalid_package_json", $"package.json is invalid JSON: {ex.Message}"));
                }
            }
            return result;
        },
        ScaffoldGenerator = request =>
        {
            var name = request.Name ?? "setimmo-node-script";
            return $$"""
            {
              "name": "{{name}}",
              "private": true,
              "scripts": {
                "build": "echo build-not-configured",
                "test": "echo test-not-configured"
              }
            }
            """;
        }
    };

    private static LanguageCapability BuildShellCapability() => new()
    {
        Key = "shell",
        DisplayName = "Shell Script",
        FileExtensions = [".sh", ".bash"],
        ProjectDetector = context => context.HasShellScripts,
        Validator = (path, content) =>
        {
            var result = CreateValidationResult(path, content);
            if (!content.StartsWith("#!/"))
            {
                result.Messages.Add(Warning("shell.missing_shebang", "Shell script has no shebang."));
            }
            if (Regex.IsMatch(content, @"\brm\s+-rf\b", RegexOptions.IgnoreCase))
            {
                result.Valid = false;
                result.Messages.Add(Error("shell.destructive_command", "Destructive shell command detected."));
            }
            if (Regex.IsMatch(content, @"curl\b.+\|\s*(bash|sh)|wget\b.+\|\s*(bash|sh)", RegexOptions.IgnoreCase | RegexOptions.Singleline))
            {
                result.Valid = false;
                result.Messages.Add(Error("shell.remote_pipe_execution", "Remote script piping into a shell is blocked."));
            }
            return result;
        },
        ScaffoldGenerator = request =>
        {
            var description = request.Description ?? "Generated by Agente Setimmo";
            return $$"""
            #!/usr/bin/env bash
            set -euo pipefail

            echo "{{description}}"
            """;
        }
    };

    private static LanguageCapability BuildCCapability() => new()
    {
        Key = "c",
        DisplayName = "C",
        FileExtensions = [".c", ".h"],
        ProjectDetector = context => context.HasMakefile || context.HasCMakeLists,
        Validator = (path, content) =>
        {
            var result = CreateValidationResult(path, content);
            if (!HasBalancedPairs(content, '{', '}'))
            {
                result.Valid = false;
                result.Messages.Add(Error("c.unbalanced_braces", "C braces are unbalanced."));
            }
            return result;
        },
        ScaffoldGenerator = request =>
        {
            var name = ToIdentifier(request.Name ?? "generated_function");
            return $$"""
            #include <stdio.h>

            int {{name}}(void)
            {
                return 0;
            }
            """;
        }
    };

    private static LanguageCapability BuildCppCapability() => new()
    {
        Key = "cpp",
        DisplayName = "C++",
        FileExtensions = [".cpp", ".cc", ".cxx", ".hpp", ".hh", ".hxx"],
        ProjectDetector = context => context.HasMakefile || context.HasCMakeLists,
        Validator = (path, content) =>
        {
            var result = CreateValidationResult(path, content);
            if (!HasBalancedPairs(content, '{', '}'))
            {
                result.Valid = false;
                result.Messages.Add(Error("cpp.unbalanced_braces", "C++ braces are unbalanced."));
            }
            return result;
        },
        ScaffoldGenerator = request =>
        {
            var name = request.Name ?? "GeneratedType";
            return $$"""
            #pragma once

            class {{name}} {
            public:
                const char* Describe() const noexcept { return "Generated by Agente Setimmo"; }
            };
            """;
        }
    };

    private static LanguageCapability BuildCSharpCapability() => new()
    {
        Key = "csharp",
        DisplayName = "C#",
        FileExtensions = [".cs", ".csproj", ".sln", ".slnx"],
        ProjectDetector = context => context.HasDotnetSolution,
        Validator = (path, content) =>
        {
            var result = CreateValidationResult(path, content);
            if (path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) &&
                !content.Contains("class ", StringComparison.Ordinal) &&
                !content.Contains("record ", StringComparison.Ordinal) &&
                !content.Contains("interface ", StringComparison.Ordinal))
            {
                result.Messages.Add(Warning("csharp.no_type", "C# source does not declare a class, record, or interface."));
            }
            if (!HasBalancedPairs(content, '{', '}'))
            {
                result.Valid = false;
                result.Messages.Add(Error("csharp.unbalanced_braces", "C# braces are unbalanced."));
            }
            if (Regex.IsMatch(content, @"\bProcess\.Start\s*\(|\bFile\.(Delete|Move|Copy)\s*\(|\bDirectory\.Delete\s*\(", RegexOptions.IgnoreCase))
            {
                result.Valid = false;
                result.Messages.Add(Error("csharp.unreviewed_process_or_file_write", "C# content includes process execution or direct file mutation and needs an explicit allowlisted service."));
            }
            return result;
        },
        ScaffoldGenerator = request =>
        {
            var name = request.Name ?? "GeneratedType";
            return $$"""
            namespace Generated;

            public sealed class {{name}}
            {
                public string Describe() => "Generated by Agente Setimmo";
            }
            """;
        }
    };

    private static LanguageCapability BuildPythonCapability() => new()
    {
        Key = "python",
        DisplayName = "Python",
        FileExtensions = [".py"],
        ProjectDetector = context => context.HasPyProject || context.HasRequirementsTxt,
        Validator = (path, content) =>
        {
            var result = CreateValidationResult(path, content);
            if (content.Contains('\t'))
            {
                result.Messages.Add(Warning("python.tab_indentation", "Python file contains tabs; verify indentation carefully."));
            }
            if (Regex.IsMatch(content, @"\bos\.system\s*\(|\bsubprocess\.(run|Popen|call)\s*\([^\n]*shell\s*=\s*True", RegexOptions.IgnoreCase))
            {
                result.Valid = false;
                result.Messages.Add(Error("python.generic_shell", "Python content exposes generic shell execution."));
            }
            return result;
        },
        ScaffoldGenerator = request =>
        {
            var name = ToIdentifier(request.Name ?? "generated_function");
            return $$"""
            def {{name}}() -> str:
                return "Generated by Agente Setimmo"
            """;
        }
    };

    private static LanguageCapability BuildLuaCapability() => new()
    {
        Key = "lua",
        DisplayName = "Lua",
        FileExtensions = [".lua"],
        ProjectDetector = context => context.HasLuaFiles,
        Validator = (path, content) =>
        {
            var result = CreateValidationResult(path, content);
            if (CountWord(content, "function") > CountWord(content, "end"))
            {
                result.Valid = false;
                result.Messages.Add(Error("lua.missing_end", "Lua function blocks look incomplete."));
            }
            if (Regex.IsMatch(content, @"\b(os\.execute|io\.popen)\s*\(", RegexOptions.IgnoreCase))
            {
                result.Valid = false;
                result.Messages.Add(Error("lua.generic_shell", "Lua content exposes generic shell execution."));
            }
            return result;
        },
        ScaffoldGenerator = request =>
        {
            var name = ToIdentifier(request.Name ?? "generated_function");
            return $$"""
            local function {{name}}()
              return "Generated by Agente Setimmo"
            end

            return {
              {{name}} = {{name}}
            }
            """;
        }
    };

    private static LanguageCapability BuildPowerShellCapability() => new()
    {
        Key = "powershell",
        DisplayName = "PowerShell",
        FileExtensions = [".ps1", ".psm1", ".psd1"],
        Validator = (path, content) =>
        {
            var result = CreateValidationResult(path, content);
            if (Regex.IsMatch(content, @"Remove-Item\s+-Recurse", RegexOptions.IgnoreCase))
            {
                result.Valid = false;
                result.Messages.Add(Error("powershell.destructive_command", "Destructive PowerShell command detected."));
            }
            if (Regex.IsMatch(content, @"\b(Invoke-Expression|iex|Start-Process)\b|cmd\.exe|powershell\s+-", RegexOptions.IgnoreCase))
            {
                result.Valid = false;
                result.Messages.Add(Error("powershell.generic_shell", "PowerShell content exposes generic command execution."));
            }
            return result;
        },
        ScaffoldGenerator = request =>
        {
            var name = request.Name ?? "Invoke-GeneratedAction";
            return $$"""
            [CmdletBinding()]
            param()

            function {{name}} {
                Write-Host "Generated by Agente Setimmo"
            }

            {{name}}
            """;
        }
    };

    private static LanguageValidationResult CreateValidationResult(string path, string content)
    {
        var formatted = NormalizeText(content, SuggestedLineEnding(path, content));
        var result = new LanguageValidationResult
        {
            Valid = true,
            FormattedContent = formatted
        };
        AddCommonValidationFindings(result, path, content);
        return result;
    }

    private static void AddCommonValidationFindings(LanguageValidationResult result, string path, string content)
    {
        if (content.Contains('\0'))
        {
            result.Valid = false;
            result.Messages.Add(Error("common.control_character", "Content contains a null control character."));
        }

        if (content.Contains("<<<<<<<", StringComparison.Ordinal) ||
            content.Contains("=======", StringComparison.Ordinal) ||
            content.Contains(">>>>>>>", StringComparison.Ordinal))
        {
            result.Valid = false;
            result.Messages.Add(Error("common.merge_conflict_marker", "Content contains unresolved merge conflict markers."));
        }

        var extension = Path.GetExtension(path);
        if (extension.Equals(".env", StringComparison.OrdinalIgnoreCase) ||
            Path.GetFileName(path).Equals("repositories.local.json", StringComparison.OrdinalIgnoreCase))
        {
            result.Valid = false;
            result.Messages.Add(Error("common.local_secret_file", "Local secret/config files are not valid implementation targets."));
        }
    }

    private static LanguageValidationMessage Error(string code, string message) =>
        new() { Severity = "error", Code = code, Message = message };

    private static LanguageValidationMessage Warning(string code, string message) =>
        new() { Severity = "warning", Code = code, Message = message };

    public static string NormalizeText(string content, string lineEnding)
    {
        var normalized = content.Replace("\r\n", "\n").Replace('\r', '\n');
        var lines = normalized.Split('\n');
        for (var i = 0; i < lines.Length; i++)
            lines[i] = lines[i].TrimEnd();
        normalized = string.Join(lineEnding, lines).TrimEnd('\r', '\n') + lineEnding;
        return normalized;
    }

    public static string SuggestedLineEnding(string path, string content)
    {
        if (content.Contains("\r\n", StringComparison.Ordinal))
            return "\r\n";

        var extension = Path.GetExtension(path);
        return extension.Equals(".cs", StringComparison.OrdinalIgnoreCase) ||
               extension.Equals(".csproj", StringComparison.OrdinalIgnoreCase) ||
               extension.Equals(".sln", StringComparison.OrdinalIgnoreCase) ||
               extension.Equals(".slnx", StringComparison.OrdinalIgnoreCase) ||
               extension.Equals(".ps1", StringComparison.OrdinalIgnoreCase) ||
               extension.Equals(".psm1", StringComparison.OrdinalIgnoreCase) ||
               extension.Equals(".psd1", StringComparison.OrdinalIgnoreCase)
            ? "\r\n"
            : "\n";
    }

    private static bool HasBalancedPairs(string content, char openChar, char closeChar)
    {
        var balance = 0;
        foreach (var ch in content)
        {
            if (ch == openChar) balance++;
            if (ch == closeChar) balance--;
            if (balance < 0) return false;
        }

        return balance == 0;
    }

    private static int Count(string content, char value) => content.Count(ch => ch == value);

    private static int CountWord(string content, string word) =>
        Regex.Matches(content, $@"\b{Regex.Escape(word)}\b", RegexOptions.IgnoreCase).Count;

    private static string ToIdentifier(string value)
    {
        var builder = new StringBuilder();
        var titleCaseNext = false;

        foreach (var ch in value)
        {
            if (char.IsLetterOrDigit(ch))
            {
                builder.Append(titleCaseNext ? char.ToUpperInvariant(ch) : ch);
                titleCaseNext = false;
            }
            else
            {
                titleCaseNext = true;
            }
        }

        if (builder.Length == 0)
            return "generatedValue";

        if (!char.IsLetter(builder[0]) && builder[0] != '_')
            builder.Insert(0, '_');

        return builder.ToString();
    }
}
