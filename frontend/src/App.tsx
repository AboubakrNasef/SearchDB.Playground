import { NavLink, Navigate, Route, Routes } from 'react-router-dom'
import MongoSearchPage from './pages/MongoSearchPage'
import PostgresSearchPage from './pages/PostgresSearchPage'

export default function App() {
  return <div className="app"><aside className="sidebar"><a href="/postgres" className="brand"><span className="brand-mark">S</span><span>Search<span className="brand-light">Lab</span></span></a><div className="side-caption">SEARCH PROVIDERS</div><nav><NavLink to="/postgres" className={({ isActive }) => `nav-link ${isActive ? 'active' : ''}`}><span>◫</span> PostgreSQL</NavLink><NavLink to="/mongo" className={({ isActive }) => `nav-link ${isActive ? 'active' : ''}`}><span>◉</span> MongoDB Atlas</NavLink></nav><div className="sidebar-foot"><span className="online-dot" /> Local environment</div></aside><div className="main-area"><header className="topbar"><span>Search performance lab</span><span className="topbar-tag">DEMO DATA</span></header><Routes><Route path="/postgres" element={<PostgresSearchPage />} /><Route path="/mongo" element={<MongoSearchPage />} /><Route path="*" element={<Navigate to="/postgres" replace />} /></Routes></div></div>
}
