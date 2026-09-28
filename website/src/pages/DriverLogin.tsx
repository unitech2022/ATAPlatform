import { Navigate, useNavigate } from 'react-router'
import { OtpLogin } from '../components/OtpLogin'
import { useI18n } from '../i18n'
import { useAuth } from '../lib/auth'

export function DriverLogin() {
  const { t } = useI18n()
  const { session, expired, login } = useAuth()
  const navigate = useNavigate()

  if (session) return <Navigate to="/driver/portal" replace />

  return (
    <OtpLogin
      role="driver"
      expired={expired}
      eyebrow={t('login.eyebrow')}
      title={t('login.phone.title')}
      copy={t('login.phone.copy')}
      onVerified={(response) => {
        login(response)
        navigate('/driver/portal', { replace: true })
        return null
      }}
    />
  )
}
