"""Falla CI si NuGet identifica vulnerabilidades altas/críticas. No oculta fallos de consulta."""
import json,subprocess
result=subprocess.run(['dotnet','list','package','--vulnerable','--include-transitive','--format','json'],text=True,capture_output=True)
if result.returncode:raise SystemExit(result.stdout+result.stderr)
data=json.loads(result.stdout)
issues=[]
for project in data.get('projects',[]):
    for framework in project.get('frameworks',[]):
        for package in framework.get('topLevelPackages',[])+framework.get('transitivePackages',[]):
            for issue in package.get('vulnerabilities',[]):
                print(package.get('id'),issue.get('severity'),issue.get('advisoryurl',''))
                if str(issue.get('severity','')).lower() in {'high','critical'}:issues.append(package.get('id'))
if issues:raise SystemExit('Corregir dependencias antes de desplegar: '+', '.join(issues))
print('Auditoría NuGet: sin vulnerabilidades altas/críticas reportadas por la fuente consultada.')
