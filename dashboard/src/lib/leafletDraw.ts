// leaflet-draw binds to the global `L` that Leaflet's UMD build installs, so `lib/leaflet` must be
// evaluated first. Importing this module from a component guarantees the order.
import { L } from './leaflet'
import 'leaflet-draw'
import 'leaflet-draw/dist/leaflet.draw.css'
import type { TranslationKey } from '../i18n'

/** Applies the dashboard's translations to leaflet-draw's toolbar and tooltips. */
export function localizeDraw(t: (key: TranslationKey) => string) {
  const local = L.drawLocal
  local.draw.toolbar.buttons.polygon = t('drawPolygon')
  local.draw.toolbar.actions = { title: t('cancel'), text: t('cancel') }
  local.draw.toolbar.finish = { title: t('finish'), text: t('finish') }
  local.draw.toolbar.undo = { title: t('undoLastPoint'), text: t('undoLastPoint') }
  local.draw.handlers.polygon.tooltip = {
    start: t('drawPolygonStart'),
    cont: t('drawPolygonContinue'),
    end: t('drawPolygonEnd'),
  }
  local.edit.toolbar.buttons.edit = t('editPolygon')
  local.edit.toolbar.buttons.editDisabled = t('editPolygon')
  local.edit.toolbar.buttons.remove = t('clearPolygon')
  local.edit.toolbar.buttons.removeDisabled = t('clearPolygon')
  local.edit.toolbar.actions.save = { title: t('save'), text: t('save') }
  local.edit.toolbar.actions.cancel = { title: t('cancel'), text: t('cancel') }
  local.edit.toolbar.actions.clearAll = { title: t('clearPolygon'), text: t('clearPolygon') }
  local.edit.handlers.edit.tooltip = { text: t('editPolygonTip'), subtext: '' }
  local.edit.handlers.remove.tooltip = { text: t('clearPolygon') }
}

export { L }
