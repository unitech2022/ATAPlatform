import { Link } from 'react-router'
import { Button } from '../components/Button'
import { Card } from '../components/Card'
import { EmptyState } from '../components/EmptyState'
import { useLang } from '../context/lang'

export function NotFoundPage() {
  const { t } = useLang()
  return (
    <Card>
      <EmptyState
        icon="pin"
        title={t('pageNotFound')}
        description={t('pageNotFoundCopy')}
        action={
          <Link to="/">
            <Button variant="secondary" icon="home">
              {t('backHome')}
            </Button>
          </Link>
        }
      />
    </Card>
  )
}
