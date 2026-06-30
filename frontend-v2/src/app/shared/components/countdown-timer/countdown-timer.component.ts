import { Component, Input, OnInit } from '@angular/core'
import { CountdownComponent, CountdownConfig, CountdownEvent } from 'ngx-countdown';

@Component({
  selector: 'app-countdown-timer',
  standalone: true,
  imports: [CountdownComponent], // Lesson 1.4: CountdownComponent not CountdownModule (removed in v21)
  template: `
    @if (secondsLeft > 0) {
      <countdown
        class="text-base font-semibold tabular-nums"
        [class.countdown-normal]="phase === 'normal'"
        [class.countdown-warning]="phase === 'warning'"
        [class.countdown-danger]="phase === 'danger'"
        [config]="config"
        (event)="onEvent($event)" />
    } @else {
      <span class="text-sm font-medium countdown-danger">Ended</span>
    }
  `,
})
export class CountdownTimerComponent implements OnInit {
    @Input({ required: true }) endTime!: string;

    secondsLeft = 0;
    phase: 'normal' | 'warning' | 'danger' = 'normal';
    config: CountdownConfig = { leftTime: 0, format: 'HH:mm:ss', notify: [300, 30] };

    ngOnInit(): void {
        const end = new Date(this.endTime).getTime();
        this.secondsLeft = Math.max(0, Math.floor((end - Date.now()) / 1000));
        this.setPhase(this.secondsLeft);
        this.config = {
            leftTime: this.secondsLeft,
            format: this.secondsLeft >= 3600 ? 'HH:mm:ss' : 'mm:ss',
            notify: [300, 30],
        }
    }

    onEvent(e: CountdownEvent): void {
        if (e.action === 'notify') this.setPhase(e.left / 1000);
        if (e.action === 'done') this.secondsLeft = 0;
    }

    private setPhase(secs: number): void {
        if (secs <= 30) this.phase = 'danger';
        else if (secs <= 300) this.phase = 'warning';
        else this.phase = 'normal';
    }
}