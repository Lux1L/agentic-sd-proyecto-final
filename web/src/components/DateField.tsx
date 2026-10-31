import { useId, useState } from 'react'
import { Button, DatePicker, Popover, Text } from '@cloudflare/kumo'
import { CalendarBlankIcon } from '@phosphor-icons/react'
import { es } from 'react-day-picker/locale'
import { dateToIso, formatDate, isoToDate } from '../format'

type Props = { label: string; value: string; onChange: (iso: string) => void; error?: string }

/** Campo de fecha: botón con formato dd/mm/aaaa que abre un calendario de Kumo en español. */
export function DateField({ label, value, onChange, error }: Props) {
  const [open, setOpen] = useState(false)
  const errId = useId()
  const selected = isoToDate(value)
  return (
    <div className="flex flex-col gap-1.5">
      <Text as="span" bold>{label}</Text>
      <Popover.Root open={open} onOpenChange={setOpen}>
        <Popover.Trigger
          render={
            <Button
              type="button"
              variant="secondary"
              icon={CalendarBlankIcon}
              aria-label={`${label}: ${value ? formatDate(value) : 'sin seleccionar'}`}
              aria-invalid={error ? true : undefined}
              aria-describedby={error ? errId : undefined}
              className={`w-full justify-start font-normal ${error ? 'ring-kumo-danger' : ''}`}
            />
          }
        >
          {value ? formatDate(value) : 'dd/mm/aaaa'}
        </Popover.Trigger>
        <Popover.Portal>
          <Popover.Positioner>
            <Popover.Popup>
              <DatePicker
                mode="single"
                locale={es}
                selected={selected}
                defaultMonth={selected}
                onChange={(d) => {
                  if (d) {
                    onChange(dateToIso(d))
                    setOpen(false)
                  }
                }}
              />
            </Popover.Popup>
          </Popover.Positioner>
        </Popover.Portal>
      </Popover.Root>
      {error && <span id={errId} className="text-sm text-kumo-danger">{error}</span>}
    </div>
  )
}
