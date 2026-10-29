const styles: Record<string, string> = {
  IT: 'bg-indigo-50 text-indigo-700',
  HR: 'bg-emerald-50 text-emerald-700',
  Finance: 'bg-amber-50 text-amber-700',
}
const defaultStyle = 'bg-slate-100 text-slate-700'

export default function CategoryBadge({ name }: { name: string }) {
  return (
    <span className={`rounded-full px-2.5 py-1 text-xs font-medium ${styles[name] ?? defaultStyle}`}>{name}</span>
  )
}
