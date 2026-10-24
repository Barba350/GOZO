"""Comprobar los archivos REALMENTE versionados, no solo .gitignore."""
import pathlib,subprocess,re
root=pathlib.Path(__file__).resolve().parents[1]
files=subprocess.check_output(['git','ls-files'],cwd=root,text=True).splitlines()
assert files,'El repositorio no tiene archivos versionados.'
for name in files:
    p=pathlib.PurePosixPath(name)
    assert not any(x in {'App_Data','keys','backups','bin','obj','__pycache__'} for x in p.parts),name
    assert not re.search(r'\.(db|sqlite|sqlite3|pfx|pem|key)(-|$)',name,re.I),name
    assert not (p.name.startswith('.env') and p.name!='.env.example'),name
print('OK: no se versionan bases de datos, claves, backups ni .env reales.')
