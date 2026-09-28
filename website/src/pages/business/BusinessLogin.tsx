import { Navigate, useLocation, useNavigate } from 'react-router'
import { OtpLogin } from '../../components/OtpLogin'
import { useI18n } from '../../i18n'
import { useBusinessAuth } from '../../lib/auth'
import { isCorporateAdmin } from '../../lib/session'

function redirectTarget(state: unknown): string {
  if (typeof state === 'object' && state !== null && 'from' in state) {
    const from = (state as { from: unknown }).from
    if (typeof from === 'string' && from.startsWith('/business/app')) return from
  }
  return '/business/app'
}

export function BusinessLogin() {
  const { t } = useI18n()
  const { session, expired, login } = useBusinessAuth()
  const navigate = useNavigate()
  const location = useLocation()
  const target = redirectTarget(location.state)

  if (isCorporateAdmin(session)) return <Navigate to={target} replace />

  return (
    <>
      <title>{t('bizLogin.pageTitle')}</title>
      <OtpLogin
        role="corporate_admin"
        expired={expired}
        icon="building"
        eyebrow={t('bizLogin.eyebrow')}
        title={t('bizLogin.title')}
        copy={t('bizLogin.copy')}
        backTo="/business"
        backLabel={t('bizLogin.back')}
        onVerified={(response) => {
          if (!response.user.roles.includes('corporate_admin')) return t('bizLogin.notAdmin')
          login(response)
          navigate(target, { replace: true })
          return null
        }}
      />
    </>
  )
}
