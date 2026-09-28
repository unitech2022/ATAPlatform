import { Icon } from './Icon'

/** Decorative map illustration from the prototype home screen (pure CSS/SVG, no data). */
export function MapArt({ caption }: { caption: string }) {
  return (
    <div className="relative aspect-[4/3] w-full overflow-hidden rounded-3xl bg-map shadow-soft sm:aspect-[5/4]">
      <svg className="absolute inset-0 size-full" viewBox="0 0 1200 800" preserveAspectRatio="xMidYMid slice" aria-hidden="true">
        <path
          className="map-block"
          d="M-20 80 170 20l90 120-80 130L0 240ZM310 0h240l20 170-250 30-70-100ZM650 20l180 10 70 170-260 50-60-120ZM970-20l260 40v220l-240-30-70-120ZM30 340l190-40 100 150-80 160-260 20ZM400 290l210-40 90 160-50 170-260-10-80-140ZM780 290l170-40 120 130-100 180-250-20-20-150ZM1080 310l160-30v300l-190-40 20-130ZM30 700l250-50 110 170H0ZM460 650l210-20 80 190H370ZM820 620l210-30 200 160v80H780Z"
        />
        <path className="road road-wide" d="M-30 680C210 560 240 330 480 310s300 100 450-10 190-230 320-240" />
        <path
          className="road"
          d="M130-20c10 190 170 240 190 390s-70 250 0 460M700-20c-70 170 30 270 10 430s-100 230-80 420M1010-20c-50 160 30 270-20 390s-120 210-70 460M-20 180c190 10 290 90 460 40s260-90 400 10 250 40 400 80M-20 510c190 40 300-50 440 20s280 120 430 50 220-10 390 40"
        />
        <path className="route-shadow" d="M398 552c75-35 73-115 146-154 78-42 133 20 200-25 63-43 51-105 115-142" />
        <path className="route" d="M398 552c75-35 73-115 146-154 78-42 133 20 200-25 63-43 51-105 115-142" />
      </svg>
      <div className="absolute left-[31%] top-[68%] grid size-12 place-items-center rounded-full border-4 border-white bg-ink text-white shadow-float" dir="ltr">
        <Icon name="pin" className="size-6" />
      </div>
      <div className="absolute left-[69%] top-[25%] grid size-14 place-items-center rounded-full border-4 border-white bg-brand text-white shadow-float" dir="ltr">
        <Icon name="car" className="size-7" />
      </div>
      <div className="absolute inset-x-4 bottom-4 flex items-center gap-3 rounded-2xl bg-white p-3 shadow-float sm:inset-x-6 sm:bottom-6">
        <div className="grid size-11 shrink-0 place-items-center rounded-xl bg-brand text-white">
          <Icon name="car" className="size-6" />
        </div>
        <p className="min-w-0 flex-1 truncate text-sm font-bold">{caption}</p>
        <Icon name="shield" className="size-5 shrink-0 text-brand" />
      </div>
    </div>
  )
}
