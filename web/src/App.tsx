import { Route, Routes } from 'react-router-dom'
import { Layout } from './components/Layout'
import Login from './pages/Login'
import Summary from './pages/Summary'
import LeaseDetail from './pages/LeaseDetail'
import NewLease from './pages/NewLease'

export default function App() {
  return (
    <Routes>
      <Route path="/login" element={<Login />} />
      <Route element={<Layout />}>
        <Route path="/" element={<Summary />} />
        <Route path="/leases/:id" element={<LeaseDetail />} />
        <Route path="/properties/:id/new-lease" element={<NewLease />} />
      </Route>
    </Routes>
  )
}
