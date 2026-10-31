import { lazy, Suspense } from 'react'
import { Route, Routes } from 'react-router-dom'
import { Layout } from './components/Layout'
const Login = lazy(() => import('./pages/Login'))
const Summary = lazy(() => import('./pages/Summary'))
const LeaseDetail = lazy(() => import('./pages/LeaseDetail'))
const NewLease = lazy(() => import('./pages/NewLease'))

export default function App() {
  return (
    <Suspense fallback={null}>
    <Routes>
      <Route path="/login" element={<Login />} />
      <Route element={<Layout />}>
        <Route path="/" element={<Summary />} />
        <Route path="/leases/:id" element={<LeaseDetail />} />
        <Route path="/properties/:id/new-lease" element={<NewLease />} />
      </Route>
    </Routes>
    </Suspense>
  )
}
