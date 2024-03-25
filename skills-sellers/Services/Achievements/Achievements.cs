using skills_sellers.Entities;

namespace skills_sellers.Services.Achievements;

public class AchievementEach5CuisineLevels : AchievementStrategy
{
    private const int RequiredAmount = 5;
    
    public AchievementEach5CuisineLevels(int statValue, Achievement achievement) 
        : base(statValue, achievement)
    {}

    public override string Name => "Each5CuisineLevels";

    public override bool IsClaimable()
    {
        var claimableTimes = StatValue / RequiredAmount;
        return Achievement.Each5CuisineLevels < claimableTimes;
    }
    
    public override void Claim(User user)
    {
        // achievement val update
        Achievement.Each5CuisineLevels++;
        
        // user val update
        user.NbCardOpeningAvailable++;
        user.Score += 200;
    }
}

public class AchievementEach5SalleDeSportLevels : AchievementStrategy
{
    private const int RequiredAmount = 5;
    
    public AchievementEach5SalleDeSportLevels(int statValue, Achievement achievement) 
        : base(statValue, achievement)
    {}

    public override string Name => "Each5SalleDeSportLevels";

    public override bool IsClaimable()
    {
        var claimableTimes = StatValue / RequiredAmount;
        return Achievement.Each5SalleDeSportLevels < claimableTimes;
    }
    
    public override void Claim(User user)
    {
        // achievement val update
        Achievement.Each5SalleDeSportLevels++;
        
        // user val update
        user.NbCardOpeningAvailable++;
        user.Score += 200;
    }
}

public class AchievementEach5SpatioportLevels : AchievementStrategy
{
    private const int RequiredAmount = 5;
    
    public AchievementEach5SpatioportLevels(int statValue, Achievement achievement) 
        : base(statValue, achievement)
    {}

    public override string Name => "Each5SpatioportLevels";

    public override bool IsClaimable()
    {
        var claimableTimes = StatValue / RequiredAmount;
        return Achievement.Each5SpatioportLevels < claimableTimes;
    }
    
    public override void Claim(User user)
    {
        // achievement val update
        Achievement.Each5SpatioportLevels++;
        
        // user val update
        user.NbCardOpeningAvailable++;
        user.Score += 500;
    }
}

public class AchievementEach100RocketLaunched : AchievementStrategy
{
    private const int RequiredAmount = 100;
    
    public AchievementEach100RocketLaunched(int statValue, Achievement achievement) 
        : base(statValue, achievement)
    {}

    public override string Name => "Each100RocketLaunched";

    public override bool IsClaimable()
    {
        var claimableTimes = StatValue / RequiredAmount;
        return Achievement.Each100RocketLaunched < claimableTimes;
    }
    
    public override void Claim(User user)
    {
        // achievement val update
        Achievement.Each100RocketLaunched++;
        
        // user val update
        user.NbCardOpeningAvailable++;
        user.Score += 500;
    }
}

public class AchievementEach100Doublon : AchievementStrategy
{
    private const int RequiredAmount = 100;
    
    public AchievementEach100Doublon(int statValue, Achievement achievement) 
        : base(statValue, achievement)
    {}

    public override string Name => "Each100Doublon";

    public override bool IsClaimable()
    {
        var claimableTimes = StatValue / RequiredAmount;
        
        // special case for first claim
        if (StatValue == 1)
            claimableTimes = 1;
        
        return Achievement.Each100Doublon < claimableTimes;
    }
    
    public override void Claim(User user)
    {
        // achievement val update
        Achievement.Each100Doublon++;
        
        // user val update
        user.NbCardOpeningAvailable++;
        if (Achievement.Each50FailCharism > 1) 
            user.Score += 100;
        user.Score += 100;
    }
}

public class AchievementEach10Cards : AchievementStrategy
{
    private const int RequiredAmount = 10;
    
    public AchievementEach10Cards(int statValue, Achievement achievement) 
        : base(statValue, achievement)
    {}

    public override string Name => "Each10Cards";

    public override bool IsClaimable()
    {
        var claimableTimes = StatValue / RequiredAmount;
        return Achievement.Each10Cards < claimableTimes;
    }
    
    public override void Claim(User user)
    {
        // achievement val update
        Achievement.Each10Cards++;
        
        // user val update
        user.NbCardOpeningAvailable++;
        user.Score += 100;
    }
}

public class AchievementEach25CasinoWin : AchievementStrategy
{
    private const int RequiredAmount = 25;
    
    public AchievementEach25CasinoWin(int statValue, Achievement achievement) 
        : base(statValue, achievement)
    {}

    public override string Name => "Each25CasinoWin";

    public override bool IsClaimable()
    {
        var claimableTimes = StatValue / RequiredAmount;
        
        // special case for first claim 
        if (StatValue == 1)
            claimableTimes = 1;
        
        return Achievement.Each25CasinoWin < claimableTimes;
    }
    
    public override void Claim(User user)
    {
        // achievement val update
        Achievement.Each25CasinoWin++;
        
        // user val update
        user.NbCardOpeningAvailable++;
        if (Achievement.Each50FailCharism > 1) 
            user.Score += 200;
        user.Score += 100;
    }
}

public class AchievementEach100MealCooked : AchievementStrategy
{
    private const int RequiredAmount = 100;
    
    public AchievementEach100MealCooked(int statValue, Achievement achievement) 
        : base(statValue, achievement)
    {}

    public override string Name => "Each100MealCooked";

    public override bool IsClaimable()
    {
        var claimableTimes = StatValue / RequiredAmount;
        return Achievement.Each100MealCooked < claimableTimes;
    }
    
    public override void Claim(User user)
    {
        // achievement val update
        Achievement.Each100MealCooked++;
        
        // user val update
        user.NbCardOpeningAvailable++;
        user.Score += 200;
    }
}

public class AchievementEach25kCreatium : AchievementStrategy
{
    private const int RequiredAmount = 25000;
    
    public AchievementEach25kCreatium(int statValue, Achievement achievement) 
        : base(statValue, achievement)
    {}

    public override string Name => "Each25kCreatium";

    public override bool IsClaimable()
    {
        var claimableTimes = StatValue / RequiredAmount;
        return Achievement.Each25kCreatium < claimableTimes;
    }
    
    public override void Claim(User user)
    {
        // achievement val update
        Achievement.Each25kCreatium++;
        
        // user val update
        user.NbCardOpeningAvailable++;
        user.Score += 500;
    }
}

public class AchievementEach20kGold : AchievementStrategy
{
    private const int RequiredAmount = 20000;
    
    public AchievementEach20kGold(int statValue, Achievement achievement) 
        : base(statValue, achievement)
    {}

    public override string Name => "Each20kGold";

    public override bool IsClaimable()
    {
        var claimableTimes = StatValue / RequiredAmount;
        return Achievement.Each20kGold < claimableTimes;
    }
    
    public override void Claim(User user)
    {
        // achievement val update
        Achievement.Each20kGold++;
        
        // user val update
        user.NbCardOpeningAvailable++;
        user.Score += 500;
    }
}

public class AchievementEach50FailCharism : AchievementStrategy
{
    private const int RequiredAmount = 50;
    
    public AchievementEach50FailCharism(int statValue, Achievement achievement) 
        : base(statValue, achievement)
    {}

    public override string Name => "Each50FailCharism";

    public override bool IsClaimable()
    {
        var claimableTimes = StatValue / RequiredAmount;
        return Achievement.Each50FailCharism < claimableTimes;
    }
    
    public override void Claim(User user)
    {
        // achievement val update
        Achievement.Each50FailCharism++;
        
        // user val update
        user.NbCardOpeningAvailable++;
        user.Score += 200;
    }
}

public class AchievementEach5CardsWithStat10 : AchievementStrategy
{
    private const int RequiredAmount = 5;
    
    public AchievementEach5CardsWithStat10(int statValue, Achievement achievement) 
        : base(statValue, achievement)
    {}

    public override string Name => "Each5CardsWithStat10";

    public override bool IsClaimable()
    {
        var claimableTimes = StatValue / RequiredAmount;
        
        // special case for first claim 
        if (StatValue == 1)
            claimableTimes = 1;
        
        return Achievement.Each5CardsWithStat10 < claimableTimes;
    }
    
    public override void Claim(User user)
    {
        // achievement val update
        Achievement.Each5CardsWithStat10++;
        
        // user val update
        user.NbCardOpeningAvailable++;
        user.Score += 100;
    }
}

public class AchievementEachCardsFullStat : AchievementStrategy
{
    private const int RequiredAmount = 1;
    
    public AchievementEachCardsFullStat(int statValue, Achievement achievement) 
        : base(statValue, achievement)
    {}

    public override string Name => "EachCardsFullStat";

    public override bool IsClaimable()
    {
        var claimableTimes = StatValue / RequiredAmount;
        
        // special case for first claim 
        if (StatValue == 1)
            claimableTimes = 1;
        
        return Achievement.EachCardsFullStat < claimableTimes;
    }
    
    public override void Claim(User user)
    {
        // achievement val update
        Achievement.EachCardsFullStat++;
        
        // user val update
        user.NbCardOpeningAvailable += 5;
        user.Score += 500;
    }
}

public class AchievementEachCollectionsCompleted : AchievementStrategy
{
    private const int RequiredAmount = 1;
    
    public AchievementEachCollectionsCompleted(int statValue, Achievement achievement) 
        : base(statValue, achievement)
    {}

    public override string Name => "EachCollectionsCompleted";

    public override bool IsClaimable()
    {
        var claimableTimes = StatValue / RequiredAmount;
        return Achievement.EachCollectionsCompleted < claimableTimes;
    }
    
    public override void Claim(User user)
    {
        // achievement val update
        Achievement.EachCollectionsCompleted++;
        
        // user val update
        user.NbCardOpeningAvailable += 5;
        user.Score += 500;
    }
}

// attaquer et gagner pour la première fois une planète (hostile ou joueur)
public class AchievementFirstPlanetAttack : AchievementStrategy
{
    private const int RequiredAmount = 1;
    
    public AchievementFirstPlanetAttack(int statValue, Achievement achievement) 
        : base(statValue, achievement)
    {}

    public override string Name => "FirstPlanetAttack";

    public override bool IsClaimable()
        => StatValue == 1 && Achievement.FirstPlanetAttack != RequiredAmount;
    
    public override void Claim(User user)
    {
        // achievement val update
        Achievement.FirstPlanetAttack = RequiredAmount;
        
        // user val update
        user.NbCardOpeningAvailable ++;
        user.Score += 100;
    }
}

public class AchievementSurviveToAnAttack : AchievementStrategy
{
    private const int RequiredAmount = 1;
    
    public AchievementSurviveToAnAttack(int statValue, Achievement achievement) 
        : base(statValue, achievement)
    {}

    public override string Name => "SurviveToAnAttack";

    public override bool IsClaimable()
        => StatValue == 1 && Achievement.SurviveToAnAttack != RequiredAmount;
    
    public override void Claim(User user)
    {
        // achievement val update
        Achievement.SurviveToAnAttack = RequiredAmount;
        
        // user val update
        user.NbCardOpeningAvailable ++;
        user.Score += 100;
    }
}