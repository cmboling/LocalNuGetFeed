import {Component, ElementRef, ViewChild} from "@angular/core";
import {Package} from "../shared/models/package.model";
import {PackageService} from "../services/package.service";
import {ActivatedRoute, Router} from "@angular/router";
import {debounceTime, distinctUntilChanged, finalize, map} from "rxjs/operators";
import {fromEvent} from "rxjs";

@Component({
  selector: 'home',
  templateUrl: './home.html',
  styleUrls: ['./home.component.css']
})

export class HomeComponent {
  public packages: Package[] = [];
  private loading = false;
  private query = '';
  private noResults: boolean;

  constructor(private _packageService: PackageService, private router: Router, private activeRoute: ActivatedRoute) {
  }

  @ViewChild('searchRef', {read: ElementRef}) searchRef: ElementRef | any;

  ngOnInit() {
    const queryParams = this.activeRoute.snapshot.queryParams;

    if (queryParams['q']) {
      this.query = queryParams['q'];
    }
    this.search(this.query);
  }

  ngAfterViewInit() {
    fromEvent((this.searchRef.nativeElement) as any, 'input').pipe(
      map((evt: any) => evt.target.value),
      debounceTime(1100), distinctUntilChanged())
      .subscribe((text: any) => {
        this.router.navigate(['/home'], text && text.length > 0 ? {queryParams: {q: text}} : {});
        this.search(text);
      });
  }

  search(query?: string) {
    this.loading = true;

    this._packageService.search(query).pipe(
      finalize(() => {
        this.loading = false;
        this.noResults = this.packages.length === 0;
      })).subscribe((result: Package[]) => {
        this.packages = result || [];
      }, (error) => {
        this.packages.length = 0;
        console.log(error);
      }
    );
  }
}
