import { Button, Link } from '@cloudflare/kumo'
import { SignOutIcon, HouseLineIcon } from '@phosphor-icons/react'
import { Navigate, Outlet, useNavigate } from 'react-router-dom'
import { clearToken, getToken } from '../api'

export function Layout() {
  const navigate = useNavigate()
  if (!getToken()) return <Navigate to="/login" replace />
  return (
    <div className="min-h-screen bg-kumo-canvas">
      <header className="border-b border-kumo-line bg-kumo-base">
        <div className="mx-auto flex max-w-6xl items-center justify-between px-4 py-3">
          <Link href="/" variant="plain" className="flex items-center gap-2 text-lg font-semibold text-kumo-default">
            <HouseLineIcon size={20} weight="fill" className="text-kumo-brand" />
            Arriendo Familiar
          </Link>
          <Button
            variant="secondary"
            icon={SignOutIcon}
            onClick={() => {
              clearToken()
              navigate('/login')
            }}
          >
            Cerrar sesión
          </Button>
        </div>
      </header>
      <main className="mx-auto max-w-6xl px-4 py-6 text-sm">
        <Outlet />
      </main>
    </div>
  )
}
