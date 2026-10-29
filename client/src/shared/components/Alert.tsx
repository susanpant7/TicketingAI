interface AlertProps {
  message: string
  variant?: 'error' | 'success'
}

const styles = {
  error: 'bg-red-50 text-red-700 ring-red-200',
  success: 'bg-emerald-50 text-emerald-700 ring-emerald-200',
}

export default function Alert({ message, variant = 'error' }: AlertProps) {
  return (
    <p
      role={variant === 'error' ? 'alert' : 'status'}
      className={`rounded-lg px-3 py-2 text-sm ring-1 ring-inset ${styles[variant]}`}
    >
      {message}
    </p>
  )
}
