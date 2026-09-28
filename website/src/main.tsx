import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import './index.css'
import App from './App.tsx'
import { AuthProvider } from './providers/AuthProvider.tsx'
import { I18nProvider } from './providers/I18nProvider.tsx'

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <I18nProvider>
      <AuthProvider scope="driver">
        <AuthProvider scope="business">
          <App />
        </AuthProvider>
      </AuthProvider>
    </I18nProvider>
  </StrictMode>,
)
