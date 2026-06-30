import { Component, OnDestroy, OnInit, AfterViewInit, ElementRef, signal, effect, viewChild } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { DnsStats } from './stats.interface';
import { Chart } from 'chart.js/auto';

@Component({
selector: 'app-root',
templateUrl: './app.html',
styleUrl: './app.css'
})
export class App implements OnInit, OnDestroy, AfterViewInit {
 stats = signal<DnsStats>({ totalQueries: 0, blockedQueries: 0, recentBlocks: [] });
 private intervalId: any;

dnsChart = viewChild.required<ElementRef<HTMLCanvasElement>>('dnsChart');
donutChartInst: any;

constructor(private http: HttpClient) {
  effect(() => {
    const s = this.stats();
  if (this.donutChartInst) {
    const clean = Math.max(0, s.totalQueries - s.blockedQueries);
    this.donutChartInst.data.datasets[0].data = [clean, s.blockedQueries];
    this.donutChartInst.update('none');
    }
  });
}

ngOnInit() {
  this.fetchData();
  this.intervalId = setInterval(() => this.fetchData(), 2000);
}

fetchData() {
  this.http.get<DnsStats>('http://localhost:5000/api/stats').subscribe({
    next: (data) => this.stats.set(data),
    error: (err) => console.error('API Error:', err)
  });
}

ngAfterViewInit() {
  this.donutChartInst = new Chart(this.dnsChart().nativeElement, {
    type: 'doughnut',
      data: {
        labels: ['Καθαρά', 'Μπλοκαρισμένα'],
        datasets: [{
        data: [1, 0],
        backgroundColor: ['#00d2ff', '#ff4757'],
        borderWidth: 0,
        hoverOffset: 4
}]
},
    options: {
    responsive: true,
    maintainAspectRatio: false,
    plugins: {
    legend: { position: 'right', labels: { color: '#fff', font: { size: 13 } } }
        },
          cutout: '65%'
        }
      });
}

ngOnDestroy() {
  if (this.intervalId) {
      clearInterval(this.intervalId);
    }
  }
}