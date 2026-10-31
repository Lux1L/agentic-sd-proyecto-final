import { forwardRef } from 'react'
import { Link as RouterLink } from 'react-router-dom'
import type { LinkComponentProps } from '@cloudflare/kumo'

// Conecta los Link/Breadcrumbs de Kumo con el router (navegación sin recarga)
export const AppLink = forwardRef<HTMLAnchorElement, LinkComponentProps>(({ href, ...rest }, ref) => (
  <RouterLink ref={ref} to={href ?? ''} {...rest} />
))
AppLink.displayName = 'AppLink'
