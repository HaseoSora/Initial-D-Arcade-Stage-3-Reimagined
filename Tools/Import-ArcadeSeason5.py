#!/usr/bin/env python3
"""Add finished Season 5 meters/ornaments from the verified local recovery.

Requires the audit's decoded materials/widget trees and textured model recovery.
Source files are read-only. Stable IDs and unrelated existing content are retained.
"""
from __future__ import annotations
import argparse
import importlib.util
import json
import re
import runpy
import shutil
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
NEW_METERS = set(range(90, 118)) - {113, 114}
UPDATED_METERS = {32, 39, 46, 47, 48, 62}
EXCLUDED_ORNAMENTS = {779, 780}


def read(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))


def write(path, data):
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(data, ensure_ascii=False, separators=(',', ':'), allow_nan=False) + '\n', encoding='utf-8')


def module(name, path):
    spec = importlib.util.spec_from_file_location(name, path)
    result = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(result)
    return result


def normalized(value):
    """Translate the audit's typed property records without inventing defaults."""
    if isinstance(value, list):
        return [normalized(v) for v in value]
    if not isinstance(value, dict):
        return value
    if 'arrayStructType' in value:
        return [normalized(v) for v in value['items']]
    if 'fields' in value and 'structType' in value:
        return normalized(value['fields'])
    if value.get('structType') in ('LinearColor', 'Color') and 'values' in value:
        return dict(zip('RGBA', value['values']))
    return {k: normalized(v) for k, v in value.items()}


def effective(materials, ref):
    chain, seen = [], set()
    while ref:
        if ref in seen or ref not in materials:
            raise ValueError('Missing/cyclic material parent: ' + ref)
        seen.add(ref)
        chain.append(ref)
        ref = materials[ref].get('parent')
    result = {k: {} for k in ('scalar', 'vector', 'texture', 'switch')}
    for ref in reversed(chain):
        item = materials[ref]
        for expression in item.get('expressions', []):
            props, cls = expression['properties'], expression['class']
            name = props.get('ParameterName')
            kind = next((kind for token, kind in [('Texture', 'texture'), ('Scalar', 'scalar'), ('Vector', 'vector'), ('StaticSwitch', 'switch')] if token in cls), None)
            if not name or not kind:
                continue
            field = 'Texture' if kind == 'texture' else 'DefaultValue'
            result[kind][name] = dict(value=props.get(field), serialized=field in props, source=ref)
        for field, kind in [('ScalarParameterValues', 'scalar'), ('VectorParameterValues', 'vector'), ('TextureParameterValues', 'texture')]:
            for parameter in item['properties'].get(field, []):
                name = parameter.get('ParameterInfo', {}).get('Name', parameter.get('ParameterName'))
                if name:
                    result[kind][name] = dict(value=parameter.get('ParameterValue'), serialized='ParameterValue' in parameter, source=ref)
    return chain, result


def strings(value):
    if isinstance(value, str):
        yield value
    elif isinstance(value, list):
        for item in value:
            yield from strings(item)
    elif isinstance(value, dict):
        for item in value.values():
            yield from strings(item)


def excluded(path):
    return bool(re.search(r'/Meter/(113|114)/', '/' + path))


def prepare_meters(audit, previous, prepared):
    data = read(previous / 'Catalog/Meter Audit/materials.json')
    materials = data['materials']
    for package in read(audit / 'materials.json')['packages']:
        if '/UI/' not in package['path'] or excluded(package['path']):
            continue
        stem = package['path'].removeprefix('GameProject/Content/').removesuffix('.uasset')
        for export in package['exports']:
            cls = export['className'].rsplit('.', 1)[-1]
            if cls not in ('Material', 'MaterialInstance', 'MaterialInstanceConstant'):
                continue
            ref = '/Game/' + stem + '.' + export['name']
            props = normalized(export['properties'])
            materials[ref] = dict(object=ref, properties=props, parent=props.get('Parent'),
                expressions=[dict(object=e['name'], **{'class': e['className'].rsplit('.', 1)[-1]}, properties=normalized(e['properties']))
                    for e in package['exports'] if 'MaterialExpression' in e['className']])
    for ref, material in materials.items():
        chain, params = effective(materials, ref)
        material.update(parentChain=chain, effectiveParameters=params,
            blendAndDomainEvidence=[dict(object=p, **{k: v for k, v in materials[p]['properties'].items()
                if k in ('BlendMode', 'MaterialDomain', 'BasePropertyOverrides')}) for p in chain])

    # Original recovery supplies byte-identical shared files; the final patched
    # Season 5 export takes precedence. Do not copy selection icons into the HUD.
    exports = {}
    for source in (previous / 'Exports', audit / 'Exports'):
        for path in source.rglob('*'):
            if path.suffix.lower() not in ('.png', '.hdr'):
                continue
            relative = path.relative_to(source).as_posix()
            if excluded(relative):
                continue
            exports[relative] = path
    wanted = set(data['textures'])
    wanted.update(v for v in strings(materials) if v.startswith('/Game/') and '.' in v)
    # Includes constructor-selected day/night/RPM alternatives, even if they
    # are not the template widget's initially selected material.
    for relative in exports:
        if relative.startswith('IND/UI/Race/Meter/'):
            path = Path(relative).with_suffix('').as_posix()
            wanted.add('/Game/' + path + '.' + Path(path).name)
    texture_data = {}
    for ref in sorted(wanted):
        prefix = ref.split('.')[0].removeprefix('/Game/')
        relative = next((prefix + ext for ext in ('.png', '.hdr') if prefix + ext in exports), None)
        if relative is None:
            old = data['textures'].get(ref)
            if old:
                relative = next((p for p in old.get('conversion', {}).get('outputs', []) if p in exports), None)
        if relative is None:
            continue
        source, target = exports[relative], prepared / 'Exports' / relative
        target.parent.mkdir(parents=True, exist_ok=True)
        if not target.exists() or source.read_bytes() != target.read_bytes():
            shutil.copyfile(source, target)
        texture_data[ref] = dict(conversion=dict(outputs=[relative]))
    data['textures'] = texture_data
    output = prepared / 'Catalog/Meter Audit'
    write(output / 'materials.json', data)
    rows = []
    for row in read(audit / 'meter-registry.json'):
        if row['id'] not in NEW_METERS | UPDATED_METERS:
            continue
        rows.append(dict(row=str(row['id']), selectedClass=row['classRef'], properties=dict(Name_source=row['properties']['Name'])))
        widget = row['classRef'].rsplit('/', 1)[-1].split('.')[0] + '.json'
        target = output / 'widgets-details' / widget
        target.parent.mkdir(parents=True, exist_ok=True)
        shutil.copyfile(audit / 'widgets-details' / widget, target)
    assert {int(r['row']) for r in rows} == NEW_METERS | UPDATED_METERS
    write(output / 'meter-registry.json', dict(rows=rows))
    shutil.copyfile(ROOT / 'Tools/ArcadeSeason5MeterRegistrations.json', output / 'runtime-registrations.json')


def import_meters(audit, previous, proof):
    prepared = proof / 'Meter Source'
    prepare_meters(audit, previous, prepared)
    source = module('meter_import', ROOT / 'Tools/Import-ArcadeMeters.py')
    output = ROOT / 'Assets/Resources/ArcadeHud/Catalog'
    importer = source.Importer(prepared, output, proof)
    importer.import_textures()
    alpha = importer.import_alpha_bounds()
    imported = [importer.import_meter(row) for row in importer.registry]
    catalog = read(output / 'catalog.json')
    by_id = {m['id']: m for m in catalog['meters']}
    for meter in imported:
        by_id[meter['id']] = meter
        if any(not l['texture'] and not l['disabledReason'] for l in meter['layers']):
            raise ValueError('Missing texture in meter ' + str(meter['id']))
    assert not importer.missing_textures, sorted(importer.missing_textures)
    assert len(by_id) == 113 and not {113, 114} & by_id.keys()
    catalog['meters'] = [by_id[i] for i in sorted(by_id)]
    write(output / 'catalog.json', catalog)
    runpy.run_path(str(ROOT / 'Tools/recover_meter_speed_colors.py'), run_name='__main__')
    write(proof / 'meter-import.json', dict(importedIds=sorted(m['id'] for m in imported), meterCount=len(by_id),
        textures=importer.texture_records, alphaBounds=alpha, unresolvedTextures=sorted(importer.missing_textures),
        layerCount=sum(len(m['layers']) for m in imported),
        disabledLayers=[dict(id=m['id'], name=l['name'], reason=l['disabledReason']) for m in imported for l in m['layers'] if l['disabledReason']]))


def import_ornaments(audit, proof):
    source = module('ornament_import', ROOT / 'Tools/Import-ArcadeOrnaments.py')
    output = ROOT / 'Assets/Resources/ArcadeOrnaments'
    importer = source.Importer(audit / 'Previews', output)
    catalog = read(output / 'catalog.json')
    by_id = {o['id']: o for o in catalog['ornaments']}
    plan = {o['id']: o for o in read(audit / 'Previews/ornament-render-plan.json')}
    names = read(ROOT / 'Tools/ArcadeSeason5OrnamentNames.json')
    materials = {}
    for package in read(audit / 'materials.json')['packages']:
        stem = package['path'].removeprefix('GameProject/Content/').removesuffix('.uasset')
        for export in package['exports']:
            cls = export['className'].rsplit('.', 1)[-1]
            if cls not in ('Material', 'MaterialInstance', 'MaterialInstanceConstant'):
                continue
            props = normalized(export['properties'])
            materials['/Game/' + stem + '.' + export['name']] = dict(properties=props, parent=props.get('Parent'),
                expressions=[dict(**{'class': e['className'].rsplit('.', 1)[-1]}, properties=normalized(e['properties']))
                    for e in package['exports'] if 'MaterialExpression' in e['className']])
    for row in read(audit / 'ornament-details.json'):
        if row['id'] not in plan:
            continue
        assert row['id'] not in EXCLUDED_ORNAMENTS
        model = plan[row['id']]['model'].removeprefix('Models/').removesuffix('.gltf')
        entry = importer.model(model, 'ornament_%04d' % row['id'])
        for mat, binding in zip(entry['materials'], plan[row['id']]['bindings']):
            relative = binding['material'].replace('\\', '/').removesuffix('.mat')
            ref = '/Game/' + relative + '.' + Path(relative).name
            chain, params = effective(materials, ref)
            textures = {}
            for key, parameter in params['texture'].items():
                texture = parameter.get('value')
                if not texture:
                    continue
                rel = texture.split('.')[0].removeprefix('/Game/') + '.png'
                source = audit / 'Previews/Source' / rel
                target = importer.models / rel
                if not source.is_file():
                    raise FileNotFoundError(source)
                if not target.exists():
                    target.parent.mkdir(parents=True, exist_ok=True)
                    shutil.copyfile(source, target)
                textures[key] = importer.texture(target)
                if key != 'Color':
                    meta = Path(str(output / 'Textures' / rel) + '.meta')
                    meta.write_text(meta.read_text().replace('sRGBTexture: 1', 'sRGBTexture: 0').replace('alphaIsTransparency: 1', 'alphaIsTransparency: 0'), encoding='utf-8')
            scalar = lambda name, fallback: params['scalar'].get(name, {}).get('value') if params['scalar'].get(name, {}).get('value') is not None else fallback
            mat.update(sourceMaterial=ref, sourceMaterialChain=chain, sourceParameters=params,
                sourceTextures=[dict(name=k, texture=v) for k, v in textures.items()],
                specularTexture=textures.get('Specular', ''), normalTexture=textures.get('Normal', ''),
                specularStrength=scalar('Specular_Parameter', 1), roughness=scalar('Roughness_Intensity', .6))
        entry.update(id=row['id'], name=names[str(row['id'])], sourceName=row['name'], strapId=row['strapId'], swingType=0,
            recoveredSocket=bool(row['sockets']), socketScale=.5 if row['sockets'] else 1, attachmentInferred=True)
        if row['id'] == 271:
            assert model.endswith('/Key_chain_plate_coute103_Black')
            entry['sourceMappingCorrection'] = 'Manazuru black plate; source row mistakenly selected Usui Snow.'
        by_id[row['id']] = entry
    assert len(plan) == 34 and len(by_id) == 314 and not EXCLUDED_ORNAMENTS & by_id.keys()
    catalog['ornaments'] = [by_id[i] for i in sorted(by_id)]
    write(output / 'catalog.json', catalog)
    for folder in (output, *(p for p in output.rglob('*') if p.is_dir())):
        importer.meta(folder)
    write(proof / 'ornament-import.json', dict(importedIds=sorted(plan), ornamentCount=len(by_id), files=importer.records))


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--audit', type=Path, required=True)
    parser.add_argument('--previous-hud', type=Path, default=Path('D:/Initial D games/Extracted HUD Assets - The Arcade S3'))
    parser.add_argument('--proof', type=Path, default=ROOT / 'Verification/arcade-s5-import-20260926')
    args = parser.parse_args()
    import_meters(args.audit, args.previous_hud, args.proof)
    import_ornaments(args.audit, args.proof)
    print('Imported 26 meters, six updated meter bindings, and 34 ornaments; placeholders excluded.')


if __name__ == '__main__':
    main()
