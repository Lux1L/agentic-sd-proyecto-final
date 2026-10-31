import { useState, type FormEvent } from 'react'
import { Button, Input, LayerCard, Text } from '@cloudflare/kumo'
import { useNavigate } from 'react-router-dom'
import { api, setToken } from '../api'
import { useErrorHandler } from '../errors'

export default function Login() {
  const navigate = useNavigate()
  const handle = useErrorHandler()
  const [email, setEmail] = useState('admin@arriendo.pe')
  const [password, setPassword] = useState('Demo1234!')
  const [errors, setErrors] = useState<Record<string, string>>({})
  const [busy, setBusy] = useState(false)

  async function submit(e: FormEvent) {
    e.preventDefault()
    setBusy(true)
    try {
      const r = await api<{ accessToken: string }>('/users/login', { method: 'POST', body: { email, password } })
      setToken(r.accessToken)
      navigate('/', { replace: true })
    } catch (err) {
      setErrors(handle(err))
    } finally {
      setBusy(false)
    }
  }

  return (
    <div className="flex min-h-screen items-center justify-center bg-kumo-canvas p-4">
      <LayerCard className="w-full max-w-sm">
        <LayerCard.Secondary>Arriendo Familiar</LayerCard.Secondary>
        <LayerCard.Primary>
          <form onSubmit={submit} className="flex flex-col gap-4">
            <Text variant="secondary">Ingresa para administrar tus alquileres.</Text>
            <Input label="Correo" type="email" value={email} onChange={(e) => setEmail(e.target.value)}
              error={errors.email} variant={errors.email ? 'error' : 'default'} />
            <Input label="Contraseña" type="password" value={password} onChange={(e) => setPassword(e.target.value)}
              error={errors.password} variant={errors.password ? 'error' : 'default'} />
            <Button type="submit" variant="primary" loading={busy}>Iniciar sesión</Button>
          </form>
        </LayerCard.Primary>
      </LayerCard>
    </div>
  )
}
