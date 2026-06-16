import { Component, input, computed } from '@angular/core';
import { CountdownComponent, CountdownConfig, CountdownEvent } from 'ngx-countdown';

@Component({
  selector: 'app-countdown-timer',
  standalone: true,
  imports: [CountdownComponent],
  template: `
    <countdown
      [config]="countdownConfig()"
      [class]="timerClass()"
      (event)="onCountdownEvent($event)">
    </countdown>
  `,
})
export class CountdownTimerComponent {
  endsAt = input.required<string>();

  private secondsRemaining = computed(() =>
    Math.max(0, Math.floor((new Date(this.endsAt()).getTime() - Date.now()) / 1000))
  );

  countdownConfig = computed<CountdownConfig>(() => ({
    leftTime: this.secondsRemaining(),
    format: this.secondsRemaining() >= 3600 ? 'HH:mm:ss' : 'mm:ss',
  }));

  timerClass = computed(() => {
    const s = this.secondsRemaining();
    if (s <= 30) return 'timer-danger font-mono font-bold text-lg';
    if (s <= 300) return 'timer-warning font-mono font-bold text-lg';
    return 'timer-normal font-mono font-bold text-lg';
  });

  onCountdownEvent(event: CountdownEvent): void {
    // event.action === 'done' when timer reaches zero
  }
}
