import { animate, query, stagger, style, transition, trigger } from '@angular/animations';

export const fadeInUp = trigger('fadeInUp', [
  transition(':enter', [
    style({ opacity: 0, transform: 'translateY(10px)' }),
    animate('260ms cubic-bezier(0.2, 0.7, 0.2, 1)', style({ opacity: 1, transform: 'translateY(0)' })),
  ]),
]);

export const cascadeIn = trigger('cascadeIn', [
  transition('* => *', [
    query(':enter', [
      style({ opacity: 0, transform: 'translateY(12px)' }),
      stagger(50, [
        animate('320ms cubic-bezier(0.2, 0.7, 0.2, 1)',
          style({ opacity: 1, transform: 'translateY(0)' })),
      ]),
    ], { optional: true }),
  ]),
]);
