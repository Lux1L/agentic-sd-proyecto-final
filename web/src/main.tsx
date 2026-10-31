import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { BrowserRouter } from 'react-router-dom'
import { LinkProvider, Toasty } from '@cloudflare/kumo'
import { AppLink } from './components/AppLink.tsx'
import './index.css'
import App from './App.tsx'

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <Toasty>
      <LinkProvider component={AppLink}>
        <BrowserRouter>
          <App />
        </BrowserRouter>
      </LinkProvider>
    </Toasty>
  </StrictMode>,
)
